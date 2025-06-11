using System.Globalization;
using Markdig;
using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;

namespace PortfolioApp.Services;

public class BlogService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<BlogService> _logger;
    private readonly MarkdownPipeline _markdownPipeline;
    private const string PostsDirectory = "wwwroot/posts";

    public BlogService(ApplicationDbContext context, IWebHostEnvironment env, ILogger<BlogService> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
        
        // Configure Markdig pipeline
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseBootstrap()
            .UseEmojiAndSmiley()
            .UseAutoLinks()
            .Build();
    }

    public async Task<IEnumerable<BlogPost>> GetPublishedPostsAsync(int? count = null)
    {
        var query = _context.BlogPosts
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.CreatedAt);
            
        if (count.HasValue)
        {
            return await query.Take(count.Value).ToListAsync();
        }
        
        return await query.ToListAsync();
    }

    public async Task<BlogPost?> GetPostBySlugAsync(string slug)
    {
        return await _context.BlogPosts
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
    }

    public async Task ProcessMarkdownFilesAsync()
    {
        try
        {
            var postsPath = Path.Combine(_env.ContentRootPath, PostsDirectory);
            
            if (!Directory.Exists(postsPath))
            {
                Directory.CreateDirectory(postsPath);
                return;
            }

            var markdownFiles = Directory.GetFiles(postsPath, "*.md");
            var existingSlugs = new HashSet<string>();

            foreach (var filePath in markdownFiles)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var slug = GenerateSlug(fileName);
                    
                    var content = await File.ReadAllTextAsync(filePath);
                    var metadata = ExtractMetadata(content, out var markdownContent);
                    
                    var existingPost = await _context.BlogPosts
                        .FirstOrDefaultAsync(p => p.Slug == slug);

                    var post = existingPost ?? new BlogPost { Slug = slug };
                    
                    // Update post properties from metadata
                    if (metadata.TryGetValue("title", out var title))
                        post.Title = title;
                        
                    if (metadata.TryGetValue("summary", out var summary))
                        post.Summary = summary;
                        
                    if (metadata.TryGetValue("image", out var imageUrl))
                        post.ImageUrl = imageUrl;
                        
                    if (metadata.TryGetValue("tags", out var tagsStr) && !string.IsNullOrEmpty(tagsStr))
                        post.Tags = tagsStr.Split(',').Select(t => t.Trim()).ToList();
                        
                    if (metadata.TryGetValue("published", out var publishedStr))
                        post.IsPublished = bool.TryParse(publishedStr, out var published) && published;
                        
                    if (metadata.TryGetValue("date", out var dateStr) && 
                        DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                        post.CreatedAt = date;
                    
                    post.Content = Markdown.ToHtml(markdownContent, _markdownPipeline);
                    post.UpdatedAt = DateTime.UtcNow;
                    
                    if (existingPost == null)
                    {
                        _context.BlogPosts.Add(post);
                    }
                    
                    existingSlugs.Add(slug);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing markdown file: {FilePath}", filePath);
                }
            }
            
            // Remove posts that no longer have corresponding markdown files
            var postsToRemove = await _context.BlogPosts
                .Where(p => !existingSlugs.Contains(p.Slug))
                .ToListAsync();
                
            if (postsToRemove.Any())
            {
                _context.BlogPosts.RemoveRange(postsToRemove);
            }
            
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing markdown files");
            throw;
        }
    }
    
    private static string GenerateSlug(string title)
    {
        // Convert to lowercase
        var slug = title.ToLowerInvariant();
        
        // Replace spaces with hyphens
        slug = slug.Replace(" ", "-");
        
        // Remove invalid characters
        slug = System.Text.RegularExpressions.Regex.Replace(slug, "[^a-z0-9-]", "");
        
        // Replace multiple hyphens with a single one
        slug = System.Text.RegularExpressions.Regex.Replace(slug, "-+", "-");
        
        // Trim hyphens from start and end
        return slug.Trim('-');
    }
    
    private static Dictionary<string, string> ExtractMetadata(string content, out string markdownContent)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var inMetadata = false;
        var contentStartLine = 0;
        
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            
            if (line == "---")
            {
                if (!inMetadata)
                {
                    inMetadata = true;
                    continue;
                }
                else
                {
                    contentStartLine = i + 1;
                    break;
                }
            }
            
            if (inMetadata)
            {
                var separatorIndex = line.IndexOf(':');
                if (separatorIndex > 0)
                {
                    var key = line[..separatorIndex].Trim().ToLowerInvariant();
                    var value = line[(separatorIndex + 1)..].Trim();
                    
                    // Remove surrounding quotes if present
                    if ((value.StartsWith("\"") && value.EndsWith("\"")) || 
                        (value.StartsWith("'") && value.EndsWith("'")))
                    {
                        value = value[1..^1];
                    }
                    
                    metadata[key] = value;
                }
            }
        }
        
        markdownContent = string.Join(Environment.NewLine, lines.Skip(contentStartLine));
        return metadata;
    }
}
