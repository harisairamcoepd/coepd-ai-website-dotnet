using Coepd.Web.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Coepd.Web.Infrastructure
{
    public static class RuntimeStore
    {
        private sealed class RuntimeStoreState
        {
            public List<Lead> Leads { get; set; } = new List<Lead>();
            public List<Staff> Staff { get; set; } = new List<Staff>();
            public int LeadId { get; set; } = 1000;
            public int StaffId { get; set; } = 100;
        }

        private static readonly object SyncRoot = new object();
        private static readonly List<Lead> Leads = new List<Lead>();
        private static readonly List<Staff> Staff = new List<Staff>();
        private static int _leadId = 1000;
        private static int _staffId = 100;
        private static string _storePath;
        private static bool _initialized;

        public static void Configure(string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory)) return;

            lock (SyncRoot)
            {
                _storePath = Path.Combine(baseDirectory, "App_Data", "runtime-store.json");
                _initialized = false;
                EnsureInitializedUnsafe();
            }
        }

        public static List<Lead> GetLeads()
        {
            lock (SyncRoot)
            {
                EnsureInitializedUnsafe();
                return Leads.Select(CloneLead).ToList();
            }
        }

        public static int AddLead(Lead lead)
        {
            if (lead == null) return 0;

            lock (SyncRoot)
            {
                EnsureInitializedUnsafe();
                var copy = CloneLead(lead);
                copy.Id = Interlocked.Increment(ref _leadId);
                if (copy.CreatedAt == default(DateTime)) copy.CreatedAt = DateTime.UtcNow;
                Leads.Add(copy);
                PersistUnsafe();
                return copy.Id;
            }
        }

        public static bool RemoveLead(int id)
        {
            lock (SyncRoot)
            {
                EnsureInitializedUnsafe();
                var existing = Leads.FirstOrDefault(x => x.Id == id);
                if (existing == null) return false;
                Leads.Remove(existing);
                PersistUnsafe();
                return true;
            }
        }

        public static Lead FindLead(int id)
        {
            lock (SyncRoot)
            {
                EnsureInitializedUnsafe();
                var existing = Leads.FirstOrDefault(x => x.Id == id);
                return existing == null ? null : CloneLead(existing);
            }
        }

        public static bool UpdateLead(Lead lead)
        {
            if (lead == null) return false;

            lock (SyncRoot)
            {
                EnsureInitializedUnsafe();
                var existing = Leads.FirstOrDefault(x => x.Id == lead.Id);
                if (existing == null) return false;
                existing.Name = lead.Name;
                existing.Phone = lead.Phone;
                existing.Email = lead.Email;
                existing.Location = lead.Location;
                existing.InterestedDomain = lead.InterestedDomain;
                existing.Whatsapp = lead.Whatsapp;
                existing.Source = lead.Source;
                existing.CreatedAt = lead.CreatedAt;
                PersistUnsafe();
                return true;
            }
        }

        public static List<Staff> GetStaff()
        {
            lock (SyncRoot)
            {
                EnsureInitializedUnsafe();
                return Staff.Select(CloneStaff).ToList();
            }
        }

        public static Staff FindStaffByEmail(string email)
        {
            var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();

            lock (SyncRoot)
            {
                EnsureInitializedUnsafe();
                var user = Staff.FirstOrDefault(x => (x.Email ?? string.Empty).Trim().ToLowerInvariant() == normalized);
                return user == null ? null : CloneStaff(user);
            }
        }

        public static int AddStaff(Staff user)
        {
            if (user == null) return 0;

            lock (SyncRoot)
            {
                EnsureInitializedUnsafe();
                var copy = CloneStaff(user);
                copy.Id = Interlocked.Increment(ref _staffId);
                if (copy.CreatedAt == default(DateTime)) copy.CreatedAt = DateTime.UtcNow;
                Staff.Add(copy);
                PersistUnsafe();
                return copy.Id;
            }
        }

        public static bool UpdateStaffStatus(int id, string status)
        {
            lock (SyncRoot)
            {
                EnsureInitializedUnsafe();
                var user = Staff.FirstOrDefault(x => x.Id == id);
                if (user == null) return false;
                user.Status = status;
                PersistUnsafe();
                return true;
            }
        }

        public static bool UpdateStaffRole(int id, string role)
        {
            lock (SyncRoot)
            {
                EnsureInitializedUnsafe();
                var user = Staff.FirstOrDefault(x => x.Id == id);
                if (user == null) return false;
                user.Role = role;
                PersistUnsafe();
                return true;
            }
        }

        public static bool RemoveStaff(int id)
        {
            lock (SyncRoot)
            {
                EnsureInitializedUnsafe();
                var user = Staff.FirstOrDefault(x => x.Id == id);
                if (user == null) return false;
                Staff.Remove(user);
                PersistUnsafe();
                return true;
            }
        }

        private static void EnsureInitializedUnsafe()
        {
            if (_initialized) return;

            if (string.IsNullOrWhiteSpace(_storePath))
            {
                _storePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "runtime-store.json");
            }

            var directory = Path.GetDirectoryName(_storePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Leads.Clear();
            Staff.Clear();
            _leadId = 1000;
            _staffId = 100;

            if (File.Exists(_storePath))
            {
                try
                {
                    var json = File.ReadAllText(_storePath);
                    var state = JsonConvert.DeserializeObject<RuntimeStoreState>(json) ?? new RuntimeStoreState();
                    Leads.AddRange((state.Leads ?? new List<Lead>()).Select(CloneLead));
                    Staff.AddRange((state.Staff ?? new List<Staff>()).Select(CloneStaff));
                    _leadId = Math.Max(state.LeadId, Leads.Any() ? Leads.Max(x => x.Id) : 1000);
                    _staffId = Math.Max(state.StaffId, Staff.Any() ? Staff.Max(x => x.Id) : 100);
                }
                catch
                {
                    Leads.Clear();
                    Staff.Clear();
                    _leadId = 1000;
                    _staffId = 100;
                }
            }

            _initialized = true;
        }

        private static void PersistUnsafe()
        {
            var state = new RuntimeStoreState
            {
                Leads = Leads.Select(CloneLead).ToList(),
                Staff = Staff.Select(CloneStaff).ToList(),
                LeadId = _leadId,
                StaffId = _staffId
            };

            File.WriteAllText(_storePath, JsonConvert.SerializeObject(state, Formatting.Indented));
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
