using Coepd.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Coepd.Web.Infrastructure
{
    public static class RuntimeStore
    {
        private static readonly object LeadLock = new object();
        private static readonly object StaffLock = new object();
        private static readonly List<Lead> Leads = new List<Lead>();
        private static readonly List<Staff> Staff = new List<Staff>();
        private static int _leadId = 1000;
        private static int _staffId = 100;

        public static List<Lead> GetLeads()
        {
            lock (LeadLock)
            {
                return Leads.Select(CloneLead).ToList();
            }
        }

        public static int AddLead(Lead lead)
        {
            if (lead == null) return 0;
            lock (LeadLock)
            {
                var copy = CloneLead(lead);
                copy.Id = Interlocked.Increment(ref _leadId);
                if (copy.CreatedAt == default(DateTime)) copy.CreatedAt = DateTime.UtcNow;
                Leads.Add(copy);
                return copy.Id;
            }
        }

        public static bool RemoveLead(int id)
        {
            lock (LeadLock)
            {
                var existing = Leads.FirstOrDefault(x => x.Id == id);
                if (existing == null) return false;
                Leads.Remove(existing);
                return true;
            }
        }

        public static List<Staff> GetStaff()
        {
            lock (StaffLock)
            {
                return Staff.Select(CloneStaff).ToList();
            }
        }

        public static Staff FindStaffByEmail(string email)
        {
            var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
            lock (StaffLock)
            {
                var user = Staff.FirstOrDefault(x => (x.Email ?? string.Empty).Trim().ToLowerInvariant() == normalized);
                return user == null ? null : CloneStaff(user);
            }
        }

        public static int AddStaff(Staff user)
        {
            if (user == null) return 0;
            lock (StaffLock)
            {
                var copy = CloneStaff(user);
                copy.Id = Interlocked.Increment(ref _staffId);
                if (copy.CreatedAt == default(DateTime)) copy.CreatedAt = DateTime.UtcNow;
                Staff.Add(copy);
                return copy.Id;
            }
        }

        public static bool UpdateStaffStatus(int id, string status)
        {
            lock (StaffLock)
            {
                var user = Staff.FirstOrDefault(x => x.Id == id);
                if (user == null) return false;
                user.Status = status;
                return true;
            }
        }

        public static bool UpdateStaffRole(int id, string role)
        {
            lock (StaffLock)
            {
                var user = Staff.FirstOrDefault(x => x.Id == id);
                if (user == null) return false;
                user.Role = role;
                return true;
            }
        }

        public static bool RemoveStaff(int id)
        {
            lock (StaffLock)
            {
                var user = Staff.FirstOrDefault(x => x.Id == id);
                if (user == null) return false;
                Staff.Remove(user);
                return true;
            }
        }

        private static Lead CloneLead(Lead x)
        {
            return new Lead
            {
                Id = x.Id,
                Name = x.Name,
                Phone = x.Phone,
                Email = x.Email,
                Location = x.Location,
                InterestedDomain = x.InterestedDomain,
                Whatsapp = x.Whatsapp,
                Source = x.Source,
                CreatedAt = x.CreatedAt
            };
        }

        private static Staff CloneStaff(Staff x)
        {
            return new Staff
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                PasswordHash = x.PasswordHash,
                Role = x.Role,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            };
        }
    }
}
