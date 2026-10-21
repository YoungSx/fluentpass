using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using NUnit.Framework;

namespace FluentPass.Tests
{
    [TestFixture, Category("Unit")]
    public class ObservableCollectionSyncTests
    {
        [Test]
        public void MembershipAndOrderChanges_KeepRetainedItemsAndAvoidReset()
        {
            var a = new object();
            var b = new object();
            var c = new object();
            var d = new object();
            var items = new ObservableCollection<object> { a, b, c };
            var actions = new List<NotifyCollectionChangedAction>();
            items.CollectionChanged += (_, e) => actions.Add(e.Action);
            var sync = typeof(FluentPassExt).Assembly.GetType("FluentPass.Common.ObservableCollectionSync")!;
            var update = sync.GetMethod("Update", BindingFlags.Public | BindingFlags.Static)!.MakeGenericMethod(typeof(object));

            update.Invoke(null, new object[] { items, new[] { c, a, d } });

            Assert.That(items, Is.EqualTo(new[] { c, a, d }));
            Assert.That(actions, Is.EqualTo(new[]
            {
                NotifyCollectionChangedAction.Remove,
                NotifyCollectionChangedAction.Move,
                NotifyCollectionChangedAction.Add
            }));

            actions.Clear();
            update.Invoke(null, new object[] { items, new[] { c, a, d } });
            Assert.That(actions, Is.Empty);

            update.Invoke(null, new object[] { items, Array.Empty<object>() });
            Assert.That(items, Is.Empty);
            Assert.That(actions, Has.All.EqualTo(NotifyCollectionChangedAction.Remove));
        }
    }
}
