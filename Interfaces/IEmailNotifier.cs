using System;

namespace CourtDocumentProcessor.Interfaces
{
    public interface IEmailNotifier
    {
        void SendNotification(string subject, string body);
    }
}