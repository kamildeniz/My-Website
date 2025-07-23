using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PortfolioApp.Filters
{
    public class AdminAuthorizeFilter : IAsyncAuthorizationFilter
    {
        private readonly ILogger<AdminAuthorizeFilter> _logger;
        private readonly IAuthorizationService _authorizationService;

        public AdminAuthorizeFilter(ILogger<AdminAuthorizeFilter> logger, IAuthorizationService authorizationService)
        {
            _logger = logger;
            _authorizationService = authorizationService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            
            if (user == null || !user.Identity.IsAuthenticated)
            {
                _logger.LogWarning("Yetkisiz erişim girişimi: Kullanıcı giriş yapmamış");
                context.Result = new RedirectToPageResult("/Identity/Account/Login", 
                    new { returnUrl = context.HttpContext.Request.Path });
                return;
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(user, "AdminPolicy");
            if (!isAuthorized.Succeeded)
            {
                _logger.LogWarning("Yetkisiz erişim girişimi: Kullanıcı admin değil");
                context.Result = new ForbidResult();
            }
        }
    }
}
