using MarketLink.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketLink.ViewComponents
{
    public class AdminSidebarViewComponent : ViewComponent
    {
        private readonly IAdminDashboardService _dashboardService;

        public AdminSidebarViewComponent(IAdminDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = await _dashboardService.GetSidebarSummaryAsync();
            return View(model);
        }
    }
}
