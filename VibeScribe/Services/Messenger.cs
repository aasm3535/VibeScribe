using System;
using System.Collections.Generic;
using System.Linq;

namespace VibeScribe.Services
{
    public class Messenger
    {
        private readonly Dictionary<Type, List<Action<object>>> _recipients = new();

        public void Register<TMessage>(Action<TMessage> action)
        {
            var messageType = typeof(TMessage);
            if (!_recipients.ContainsKey(messageType))
            {
                _recipients[messageType] = new List<Action<object>>();
            }
            _recipients[messageType].Add(message => action((TMessage)message));
        }

        public void Send<TMessage>(TMessage message)
        {
            var messageType = typeof(TMessage);
            if (_recipients.ContainsKey(messageType))
            {
                foreach (var action in _recipients[messageType].ToList())
                {
                    action(message);
                }
            }
        }
    }
}