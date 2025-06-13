using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using PortfolioApp.Services;
using System;

namespace PortfolioApp.Filters
{
    public class AdminAuthorizeFilter : IAsyncPageFilter
    {
        private readonly AuthService _authService;

        public AdminAuthorizeFilter(AuthService authService)
        {
            _authService = authService;
        }

        public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            // Skip authorization for Login page
            if (context.ActionDescriptor.RelativePath.Contains("Login"))
            {
                await next();
                return;
            }

            var isAuthenticated = await _authService.IsAuthenticatedAsync();
            if (!isAuthenticated)
            {
                context.Result = new RedirectToPageResult("/Admin/Login", new { returnUrl = context.HttpContext.Request.Path });
                return;
            }

            await next();
        }

        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            return Task.CompletedTask;
        }
    }
}
