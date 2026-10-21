# Hidden KeePass host: diagnosis and repair

The shell and KeePass remain in the same process and on the same STA thread. KeePass owns database state, hotkey registration, sequence compilation and physical Auto-Type input. FluentPass owns its current group and entry selection.

## Reproduced defect

`RefreshDatabaseState()` called `ReloadChildren()` on every host event. `ReloadChildren()` cleared and re-added every child, including unchanged groups. WPF treats `Reset` as removal of the selected container and selects an ancestor. Search refresh also cleared its bound collection. Selection setters then projected the new selection into the hidden KeePass list, potentially calling `UpdateUI()` and emitting another host event. A synchronous reentrancy flag cannot protect a separately queued refresh.

The Windows test `HostRefresh_PreservesSelectedChildInBoundWpfTree` reproduced loss of the selected child before the fix. The no-change, reorder and search tests also failed before the fix and passed afterwards. This establishes the selection defect and its feedback path; it does not establish that every reported production freeze had this cause.

## Boundaries after the repair

- Reconcile tree and entry collections using actual add/remove/move changes. Retain a ViewModel only while it holds the same live KeePass object. Never retain a detached object merely because its UUID matches.
- Browsing no longer changes the hidden native list, tree, focus or scroll position. `SynchronizeHostEntrySelection` and its compensating calls have been removed.
- A WinForms `NativeWindow` receives KeePass's existing Auto-Type messages. When the hidden host is serving the shell, selected-entry Auto-Type uses the shell's live entry directly. An empty/stale shell selection is consumed without falling back to an old native selection. Native mode and other messages retain their native handlers.
- One activity scope covers preparation, compilation, plugin callbacks, sending and native completion. Nested Auto-Type messages are consumed while busy. UI commands and the detail button use the same scope. Foreground validation rejects FluentPass and other windows in the KeePass process.
- After minimizing the WPF window, hidden-host Auto-Type calls official `PerformIntoCurrentWindow`. A visible native host continues through official `PerformIntoPreviousWindow`. Database/entry validity is rechecked after minimization and before sending.
- Host events mark one deferred refresh pending. A background dispatcher timer checks activity when executing, retains pending work while busy and stops when idle. File close clears revealed values and editing passwords synchronously. Shutdown removes the native hook, host subscriptions and timer.

## Evidence and validation boundary

The test-first Windows runs are available at [tree regression](https://github.com/YoungSx/fluentpass/actions/runs/39574866548) and [search regression](https://github.com/YoungSx/fluentpass/actions/runs/39575633518). [The first repaired run](https://github.com/YoungSx/fluentpass/actions/runs/39576518988) passed all four regression tests. Later runs add message routing, activity scope, handle lifecycle, object replacement, plaintext cleanup and hidden-host browsing checks.

Local Linux ARM64 can cross-build the real Release assemblies but cannot execute WPF. Mono tests that require PresentationFramework fail for the missing runtime; no WPF stubs count as validation. Windows CI runs the real WPF and WinForms tests using KeePass 2.57.1. Local native-only probes use installed KeePass 2.47 and are not evidence for the deployed Windows freeze.

Desktop acceptance still needs the deployed plugin and a real external target: repeatedly switch groups/search results with native KeePass hidden; exercise Ctrl+V, the detail button and KeePass's configured selected/global/password hotkeys; repeat while visible, locked and with native Options open. Verify target selection and keyboard output, including cancellation and overlapping triggers. Automated callback tests and successful builds do not prove physical keyboard injection or third-party plugin compatibility.

## Platform sources

- Microsoft: [NativeWindow.AssignHandle lifecycle example](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.nativewindow.assignhandle?view=netframework-4.8.1).
- WPF: [TreeViewItem.OnItemsChanged](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/TreeViewItem.cs), which handles removal/reset of a selected child differently from add/move.
- KeePass official source: `MainForm.HandleHotKey` / `ProcessAppMessage` / `ExecuteEntryAutoType`, `AutoType.PerformIntoCurrentWindow` / `PerformIntoPreviousWindow` / `Execute`, and `SendInputEx.SendKeysWait`. The selected-entry native handler reads native selection; the direct API takes a `PwEntry`. `IsSending` covers the send phase, while compilation and focus changes occur earlier.
