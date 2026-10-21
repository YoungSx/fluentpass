using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using KeePass.Forms;
using FluentPass.Common;
using KeePassLib;

namespace FluentPass.ViewModels
{
    public partial class GroupItemViewModel : ObservableObject
    {
        private readonly MainForm? _mainForm;
        private readonly PwDatabase? _database;

        public PwGroup Model { get; }
        public GroupItemViewModel? Parent { get; }

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private ImageSource? _iconSource;

        [ObservableProperty]
        private bool _isExpanded;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private int _entryCount;

        public ObservableCollection<GroupItemViewModel> Children { get; } = new ObservableCollection<GroupItemViewModel>();

        public GroupItemViewModel(PwGroup group, GroupItemViewModel? parent, MainForm? mainForm, PwDatabase? database)
        {
            Model = group ?? throw new ArgumentNullException(nameof(group));
            Parent = parent;
            _mainForm = mainForm;
            _database = database;

            _name = group.Name;
            _entryCount = (int)group.Entries.UCount;
            _isExpanded = parent == null; // 根节点默认展开

            RefreshIcon();
            LoadChildren();
        }

        public void RefreshIcon()
        {
            IconSource = IconConverterHelper.GetIconSource(_mainForm, _database, Model.IconId, Model.CustomIconUuid);
        }

        public void RefreshState()
        {
            Name = Model.Name;
            EntryCount = (int)Model.Entries.UCount;
            RefreshIcon();

            foreach (var child in Children)
            {
                child.RefreshState();
            }
        }

        public void RefreshSelfAndAncestors()
        {
            Name = Model.Name;
            EntryCount = (int)Model.Entries.UCount;
            RefreshIcon();
            Parent?.RefreshSelfAndAncestors();
        }

        private void LoadChildren()
        {
            Children.Clear();
            foreach (var subGroup in Model.Groups)
            {
                Children.Add(new GroupItemViewModel(subGroup, this, _mainForm, _database));
            }
        }

        public void ReloadChildren()
        {
            Name = Model.Name;
            EntryCount = (int)Model.Entries.UCount;
            RefreshIcon();

            var currentVms = new Dictionary<PwUuid, GroupItemViewModel>();
            foreach (var child in Children)
            {
                currentVms[child.Model.Uuid] = child;
            }

            var children = new List<GroupItemViewModel>();
            foreach (var subGroup in Model.Groups)
            {
                if (currentVms.TryGetValue(subGroup.Uuid, out var existing) && ReferenceEquals(existing.Model, subGroup))
                {
                    existing.ReloadChildren();
                    children.Add(existing);
                }
                else
                {
                    children.Add(new GroupItemViewModel(subGroup, this, _mainForm, _database));
                }
            }
            ObservableCollectionSync.Update(Children, children);
        }

        public void ExpandAll()
        {
            IsExpanded = true;
            foreach (var child in Children)
            {
                child.ExpandAll();
            }
        }

        public void CollapseAll()
        {
            foreach (var child in Children)
            {
                child.CollapseAll();
            }

            if (Parent != null)
            {
                IsExpanded = false;
            }
        }

        public void AddChildGroup(PwGroup newGroup)
        {
            var vm = new GroupItemViewModel(newGroup, this, _mainForm, _database);
            Children.Add(vm);
            IsExpanded = true;
            RefreshSelfAndAncestors();
        }

        public void RemoveChildGroup(GroupItemViewModel child)
        {
            Children.Remove(child);
            RefreshSelfAndAncestors();
        }
    }
}
