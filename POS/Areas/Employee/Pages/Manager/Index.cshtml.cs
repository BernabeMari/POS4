using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using POS.Data;
using POS.Models;
using POS.Services;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace POS.Areas.Employee.Pages.Manager
{
    [Authorize(Roles = "Employee,Manager,Admin")]
    public class IndexModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            IOrderService orderService, 
            IProductService productService,
            ApplicationDbContext context)
        {
            _orderService = orderService;
            _productService = productService;
            _context = context;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public string EmployeeId { get; set; }
        public string FullName { get; set; }
        public string PositionName { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Get the current employee ID
            EmployeeId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if user is in Employee role
            if (!User.IsInRole("Employee"))
            {
                return RedirectToPage("/Index");
            }

            // Get user information with position
            var user = await _context.Users
                .Include(u => u.Position)
                .FirstOrDefaultAsync(u => u.Id == EmployeeId);
                
            bool isManager = user?.Position?.Name == "Manager";
            bool isAdmin = User.IsInRole("Admin");
            
            // Check if the user is an Assistant Manager with manager access
            bool isAssistantManagerWithAccess = false;
            if (user?.Position?.Name == "Assistant Manager")
            {
                isAssistantManagerWithAccess = await _context.UserPreferences
                    .AnyAsync(p => p.UserId == EmployeeId && p.Key == "ManagerAccess" && p.Value == "true");
            }
            
            // Only allow access if user is a Manager, Admin, or an Assistant Manager with access
            if (!isManager && !isAdmin && !isAssistantManagerWithAccess)
            {
                // Redirect to the appropriate dashboard based on position
                if (user?.Position?.Name == "Assistant Manager")
                {
                    return RedirectToPage("/AssistantManager/Index", new { area = "Employee" });
                }
                else if (user?.Position?.Name == "Inventory Clerk")
                {
                    return RedirectToPage("/InventoryClerk/Index", new { area = "Employee" });
                }
                else
                {
                    return RedirectToPage("/Index", new { area = "Employee" });
                }
            }

            FullName = User.Identity?.Name ?? "Manager";
            PositionName = user?.Position?.Name ?? "Manager";

            return Page();
        }
    }
} 