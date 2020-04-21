using ClassTranscribeDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassTranscribeDatabase.Notification
{
    public class SubscriptionManager
    {
        private readonly CTDbContext _context;

        public SubscriptionManager(CTDbContext context)
        {
            _context = context;
        }

        public async Task Subscribe(Entity entity, ApplicationUser user)
        {
            Subscription subscription = new Subscription 
            {   ApplicationUserId = user.Id,
                ResourceType = entity.GetResourceType(),
                ResourceId = entity.Id
            };
            if(!(await _context.Subscriptions.AnyAsync(s => s.ResourceType == subscription.ResourceType &&
            s.ResourceId == subscription.ResourceId &&
            s.ApplicationUserId == subscription.ApplicationUserId)))
            {
                _context.Subscriptions.Add(subscription);
            }
            await _context.SaveChangesAsync();
        }

        public async Task Unsubscribe(Entity entity, ApplicationUser user)
        {
            Subscription subscription = new Subscription
            {
                ApplicationUserId = user.Id,
                ResourceType = entity.GetResourceType(),
                ResourceId = entity.Id
            };
            if (await _context.Subscriptions.AnyAsync(s => s.ResourceType == subscription.ResourceType &&
             s.ResourceId == subscription.ResourceId &&
             s.ApplicationUserId == subscription.ApplicationUserId))
            {
                _context.Subscriptions.Remove(subscription);
            }
            await _context.SaveChangesAsync();
        }

        public async Task AddMessageToSubscription(Entity entity, JObject payload, LogLevel logLevel)
        {
            var userIds = await _context.Subscriptions
                .Where(s => s.ResourceType == entity.GetResourceType() && s.Id == entity.Id)
                .Select(s => s.ApplicationUserId)
                .ToListAsync();

            var messages = userIds.Select(uId => new Message
            {
                LogLevel = logLevel,
                ApplicationUserId = uId,
                Payload = payload
            }).ToList();

            await _context.Messages.AddRangeAsync(messages);
        }

        public async Task<IEnumerable<Message>> GetNotifications(ApplicationUser user)
        {
            return await _context.Messages
                .Where(m => m.Ack == Ack.Pending && m.ApplicationUserId == user.Id)
                .ToListAsync();
        }

        public async Task AcknowledgeMessage(string messageId)
        {
            Message m = await _context.Messages.Where(m => m.Id == messageId).FirstOrDefaultAsync();
            m.Ack = Ack.Seen;
            await _context.SaveChangesAsync();
        }

        public async Task AcknowledgeMessages(List<string> messageIds)
        {
            List<Message> messages = await _context.Messages.Where(m => messageIds.Contains(m.Id)).ToListAsync();
            messages.ForEach(m =>
            {
                m.Ack = Ack.Seen;
            });
            await _context.SaveChangesAsync();
        }
    }
}
