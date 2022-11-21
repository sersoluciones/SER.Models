using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace SER.Models.SERAudit
{
    public class AuditManager
    {
        private readonly ILogger _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public AuditManager(
            ILogger<AuditManager> logger,
            IHttpContextAccessor contextAccessor)
        {
            _logger = logger;
            _contextAccessor = contextAccessor;
        }

        public async Task<string> AddLog(DbContext context, AuditBinding entity, string id = "", bool commit = false)
        {
            context.ChangeTracker.DetectChanges();
            string valuesToChange = null;
            var entities = context.ChangeTracker.Entries()
                .Where(x => x.State == EntityState.Modified
                || x.State == EntityState.Added
                || x.State == EntityState.Deleted && x.Entity != null).ToList();

            var writerOptions = new JsonWriterOptions
            {
                Indented = false
            };
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, writerOptions);
            writer.WriteStartObject();

            if (!string.IsNullOrEmpty(id))
            {
                writer.WritePropertyName("ObjectId");
                writer.WriteStringValue(id);
            }

            if (entity.extras != null)
            {
                foreach (var extra in entity.extras)
                {
                    writer.WritePropertyName(extra.Key);
                    writer.WriteStringValue(extra.Value);
                }
            }
            string entityName = "";
            if ((new int[] { (int)AudiState.CREATE, (int)AudiState.UPDATE, (int)AudiState.DELETE }).ToList().Contains((int)entity.action))
            {
                foreach (var add in entities.Where(p => p.State == EntityState.Added))
                {
                    entityName = add.Entity.GetType().Name;
                    _logger.LogInformation($"EntityState.Added, entityName {entityName}\n");
                }

                foreach (var change in entities.Where(p => p.State == EntityState.Modified))
                {
                    entityName = change.Entity.GetType().Name;
                    _logger.LogInformation($"EntityState.Modified, entityName {entityName}\n");
                    //if (!(new string[] { "Claim" }).ToList().Contains(entity.Object))
                    //    entity.Object = entityName;
                    //entity.Action = UPDATE;
                    valuesToChange = AuditEntityModified(change, writer: writer);
                }

                foreach (var delete in entities.Where(p => p.State == EntityState.Deleted))
                {
                    entityName = delete.Entity.GetType().Name;
                    _logger.LogInformation($"EntityState.Deleted, entityName {entityName}\n");
                }
            }
            var userId = GetCurrentUser();
            var userName = GetCurrenUserName();
            writer.WriteEndObject();
            writer.Flush();
            var data = Encoding.UTF8.GetString(stream.ToArray());

            var log = new Audit()
            {
                date = DateTime.UtcNow,
                action = entity.action,
                objeto = entity.objeto,
                username = userName,
                role = string.Join(",", GetRolesUser().ToArray()),
                json_browser = InfoBrowser(_contextAccessor),
                json_request = GetInfoRequest(_contextAccessor),
                data = data,
                user_id = userId
            };

            await context.Set<Audit>().AddAsync(log);

            var json = JsonSerializer.Serialize<object>(
                new
                {
                    CurrentDate = DateTime.UtcNow,
                    entity.action,
                    entity.objeto,
                },
                new JsonSerializerOptions { WriteIndented = true, });

            // await SendMsgSignalR(json);
            if (commit) await context.SaveChangesAsync();

            return valuesToChange;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private Task SendMsgSignalR(string msg)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            //if (GetCurrenUserName() != null)
            //    await _hub.Clients.User(GetCurrenUserName())
            //        .SendAsync("ReceiveMessage", msg);
            return Task.FromResult(0);
        }

        public static string GetEntityChanges(DbContext context)
        {
            context.ChangeTracker.DetectChanges();
            string valuesToChange = null;
            var entities = context.ChangeTracker.Entries()
                .Where(x => x.State == EntityState.Modified
                || x.State == EntityState.Added
                || x.State == EntityState.Deleted && x.Entity != null).ToList();

            foreach (var change in entities.Where(p => p.State == EntityState.Modified))
            {
                string entityName = change.Entity.GetType().Name;
                valuesToChange = AuditEntityModified(change);
            }

            return valuesToChange;
        }

        public static string AuditEntityModified(EntityEntry objectStateEntry, Utf8JsonWriter writer = null)
        {
            writer?.WritePropertyName("Values");
            writer?.WriteStartArray();
            var values = new List<AuditUpdate>();
            foreach (var prop in objectStateEntry.OriginalValues.Properties)
            {
                string originalValue = null;
                if (objectStateEntry.OriginalValues[prop] != null)
                    originalValue = objectStateEntry.OriginalValues[prop].ToString();
                string currentValue = null;
                if (objectStateEntry.CurrentValues[prop] != null)
                    currentValue = objectStateEntry.CurrentValues[prop].ToString();

                if (originalValue != currentValue) //Only create a log if the value changes
                {
                    values.Add(new AuditUpdate
                    {
                        PropertyName = prop.Name,
                        OldValue = originalValue,
                        NewValue = currentValue,
                    });
                    writer?.WriteStartObject();
                    writer?.WritePropertyName("PropertyName");
                    writer?.WriteStringValue(prop.Name);

                    writer?.WritePropertyName("OldValue");
                    writer?.WriteStringValue(originalValue);

                    writer?.WritePropertyName("NewValue");
                    writer?.WriteStringValue(currentValue);
                    writer?.WriteEndObject();
                }
            }
            writer?.WriteEndArray();

            return JsonSerializer.Serialize(values);
        }


        public static string InfoBrowser(IHttpContextAccessor httpContextAccessor)
        {
            try
            {
                string userAgent = httpContextAccessor.HttpContext.Request.Headers["User-Agent"];
                UserAgent ua = new(userAgent);
                return JsonSerializer.Serialize(ua);
            }
            catch (Exception) { }
            return "";
        }

        public static string GetInfoRequest(IHttpContextAccessor httpContextAccessor)
        {
            string refer = httpContextAccessor.HttpContext.Request.Headers["Referer"];
            var infoRequest = new InfoRequest
            {
                verb = string.Format("{0}", httpContextAccessor.HttpContext.Request.Method),
                content_type = string.Format("{0}", httpContextAccessor.HttpContext.Request.ContentType),
                encoded_url = string.Format("{0}", httpContextAccessor.HttpContext.Request.GetEncodedUrl()),
                path = string.Format("{0}", httpContextAccessor.HttpContext.Request.Path),
                remote_ip_address = string.Format("{0}", httpContextAccessor.HttpContext.Connection.RemoteIpAddress),
                host = string.Format("{0}", httpContextAccessor.HttpContext.Request.Host),
                refferer_url = string.Format("{0}", (string.IsNullOrEmpty(refer)) ? "" : refer)
            };
            return JsonSerializer.Serialize<InfoRequest>(infoRequest);
        }

        class InfoRequest
        {
            public string verb { get; set; }
            public string content_type { get; set; }
            public string encoded_url { get; set; }
            public string path { get; set; }
            public string remote_ip_address { get; set; }
            public string host { get; set; }
            public string refferer_url { get; set; }
        }

        public string GetCurrentUser()
        {
            return _contextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
        }

        public string GetCurrenUserName()
        {
            return _contextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "name")?.Value;
        }

        public List<string> GetRolesUser()
        {
            return _contextAccessor.HttpContext.User.Claims.Where(x =>
                x.Type == "role").Select(x => x.Value).ToList();
        }

        public static JsonElement ToJsonDocument(string response)
        {
            var documentOptions = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip
            };
            return JsonDocument.Parse(response, documentOptions).RootElement;
        }
    }

    class AuditUpdate
    {

        public string PropertyName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
    }
}
