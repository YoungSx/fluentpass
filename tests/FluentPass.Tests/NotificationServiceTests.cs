using System;
using System.Threading;
using System.Threading.Tasks;
using FluentPass.Common;
using NUnit.Framework;
using Wpf.Ui.Controls;

namespace FluentPass.Tests
{
    [TestFixture, Apartment(ApartmentState.STA)]
    public class NotificationServiceTests
    {
        [SetUp]
        public void Setup()
        {
            NotificationService.ClearHistory();
        }

        [TearDown]
        public void TearDown()
        {
            NotificationService.ClearHistory();
        }

        [Test]
        public void ShowSuccess_RecordsNotificationCorrectly()
        {
            NotificationService.ShowSuccess("Success Title", "Operation completed successfully.", 4);

            var last = NotificationService.LastNotification;
            Assert.That(last, Is.Not.Null);
            Assert.That(last!.Title, Is.EqualTo("Success Title"));
            Assert.That(last.Message, Is.EqualTo("Operation completed successfully."));
            Assert.That(last.Appearance, Is.EqualTo(ControlAppearance.Success));
            Assert.That(last.Timeout, Is.EqualTo(TimeSpan.FromSeconds(4)));
            Assert.That(last.Icon, Is.Not.Null);
        }

        [Test]
        public void ShowInformation_RecordsNotificationCorrectly()
        {
            NotificationService.ShowInformation("Info Title", "Here is some info.");

            var last = NotificationService.LastNotification;
            Assert.That(last, Is.Not.Null);
            Assert.That(last!.Title, Is.EqualTo("Info Title"));
            Assert.That(last.Message, Is.EqualTo("Here is some info."));
            Assert.That(last.Appearance, Is.EqualTo(ControlAppearance.Info));
            Assert.That(last.Timeout, Is.EqualTo(TimeSpan.FromSeconds(3)));
        }

        [Test]
        public void ShowWarning_RecordsNotificationCorrectly()
        {
            NotificationService.ShowWarning("Warning Title", "Be careful!");

            var last = NotificationService.LastNotification;
            Assert.That(last, Is.Not.Null);
            Assert.That(last!.Title, Is.EqualTo("Warning Title"));
            Assert.That(last.Appearance, Is.EqualTo(ControlAppearance.Caution));
        }

        [Test]
        public void ShowError_RecordsNotificationCorrectly()
        {
            NotificationService.ShowError("Error Title", "Something failed!");

            var last = NotificationService.LastNotification;
            Assert.That(last, Is.Not.Null);
            Assert.That(last!.Title, Is.EqualTo("Error Title"));
            Assert.That(last.Appearance, Is.EqualTo(ControlAppearance.Danger));
            Assert.That(last.Timeout, Is.EqualTo(TimeSpan.FromSeconds(5)));
        }

        [Test]
        public async Task Show_FromBackgroundThread_IsThreadSafe()
        {
            int taskCount = 10;
            var tasks = new Task[taskCount];

            for (int i = 0; i < taskCount; i++)
            {
                int index = i;
                tasks[i] = Task.Run(() =>
                {
                    NotificationService.ShowSuccess($"Thread Title {index}", $"Message from thread {Thread.CurrentThread.ManagedThreadId}");
                });
            }

            await Task.WhenAll(tasks);

            var history = NotificationService.History;
            Assert.That(history.Count, Is.EqualTo(taskCount));
        }

        [Test]
        public void ClearHistory_ResetsHistoryAndLastNotification()
        {
            NotificationService.ShowInformation("Title 1", "Msg 1");
            Assert.That(NotificationService.History.Count, Is.EqualTo(1));
            Assert.That(NotificationService.LastNotification, Is.Not.Null);

            NotificationService.ClearHistory();

            Assert.That(NotificationService.History.Count, Is.EqualTo(0));
            Assert.That(NotificationService.LastNotification, Is.Null);
        }
    }
}
