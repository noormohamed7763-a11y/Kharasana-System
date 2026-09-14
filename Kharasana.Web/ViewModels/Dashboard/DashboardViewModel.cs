using System;
using System.Collections.Generic;

namespace Kharasana.Web.ViewModels.Dashboard
{
    public class DashboardViewModel
    {
        // ==========================
        // Admin Dashboard KPIs
        // ==========================
        public int TotalFactories { get; set; }
        public int TotalClients { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalUsers { get; set; }
        public int TotalDrivers { get; set; }
        public int TotalOrders { get; set; }

        // ==========================
        // Factory Dashboard KPIs
        // ==========================
        public int TodayOrdersCount { get; set; }
        public int InProgressOrdersCount { get; set; }
        public int ReadyOrdersCount { get; set; }
        public int OnTheWayOrders { get; set; }
        public int DeliveredToday { get; set; }
        public int AvailableDrivers { get; set; }

        // ==========================
        // Tables & Charts Extensions
        // ==========================
        public List<RecentOrderDto> RecentOrders { get; set; } = new();
        public string? WeeklyOrdersJson { get; set; }
        public string? OrderStatusJson { get; set; }
    }

    public class RecentOrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string StatusDisplay { get; set; } = string.Empty;
        public string StatusCssClass { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }

        /// <summary>
        /// الجزء التسلسلي من رقم الطلب (ORD-yyyyMMdd-seq → seq)
        /// </summary>
        public string OrderNumberSeq
        {
            get
            {
                var parts = OrderNumber.Split('-');
                return parts.Length >= 3 ? parts[2] : OrderNumber;
            }
        }

        /// <summary>
        /// جزء التاريخ من رقم الطلب (ORD-yyyyMMdd-seq → yyyy-MM-dd)
        /// </summary>
        public string OrderNumberDate
        {
            get
            {
                var parts = OrderNumber.Split('-');
                if (parts.Length >= 3 && parts[1].Length == 8)
                {
                    return $"{parts[1][..4]}-{parts[1][4..6]}-{parts[1][6..8]}";
                }
                return string.Empty;
            }
        }
    }
}