using System.Threading;
using System.Threading.Tasks;
using MarketLink.ViewModels;

namespace MarketLink.Services
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);
        Task<AdminSidebarSummaryViewModel> GetSidebarSummaryAsync(CancellationToken cancellationToken = default);
    }
}
