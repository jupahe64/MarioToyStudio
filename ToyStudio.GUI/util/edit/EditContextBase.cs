using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.GUI.util.edit.undo_redo;
using static System.Collections.Specialized.BitVector32;

namespace ToyStudio.GUI.util.edit
{
    interface ICommittable
    {
        void Commit(string name);
    }

    internal class EditContextBase
    {
        public object? ActiveObject => _activeObject;

        public ulong SelectionVersion { get; private set; } = 0;
        public int SelectedObjectCount => _selectedObjects.Count;

        public event Action? Update;

        public void BatchAction(Func<string> actionReturningName)
            => WithSuspendUpdateDo(() =>
        {
            if (_batchActionNestingLevel == 0)
                _currentActionBatch = [];

            _batchActionNestingLevel++;
            var actionName = actionReturningName.Invoke();
            _batchActionNestingLevel--;

            if (_batchActionNestingLevel > 0)
                return;

            Debug.Assert(_currentActionBatch is not null);
            _undoHandler.AddToUndo(_currentActionBatch, actionName);
            _currentActionBatch = null;
            DoUpdate();
        });

        public void Commit(IRevertable action)
        {
            if (_currentActionBatch is not null)
            {
                _currentActionBatch.Add(action);
                return;
            }

            _undoHandler.AddToUndo(action);
            DoUpdate();
        }

        public void Deselect(object obj)
        {
            int countBefore = _selectedObjects.Count;
            _selectedObjects.Remove(obj);

            if (_selectedObjects.Count != countBefore)
                SelectionChanged();
        }

        public void DeselectAll()
        {
            if (_selectedObjects.Count == 0)
                return;

            _selectedObjects.Clear();
            SelectionChanged();
        }

        public void DeselectAllOfType<T>()
            where T : class
        {
            int countBefore = _selectedObjects.Count;
            _selectedObjects.RemoveWhere(x => x is T);

            if (_selectedObjects.Count != countBefore)
                SelectionChanged();
        }


        public object? GetLastAction() => _undoHandler.GetLastAction();
        public IEnumerable<IRevertable> GetRedoUndoStack() => _undoHandler.GetRedoUndoStack();

        public IEnumerable<T> GetSelectedObjects<T>()
            where T : class
            => _selectedObjects.OfType<T>();

        //For Undo Window
        public IEnumerable<IRevertable> GetUndoStack() => _undoHandler.GetUndoStack();

        public bool IsAnySelected<T>()
            where T : class
        {
            return _selectedObjects.Any(x => x is T);
        }

        public bool IsSelected(object obj) =>
            _selectedObjects.Contains(obj);

        public bool IsSingleObjectSelected(object obj) =>
            _selectedObjects.Count == 1 && _selectedObjects.Contains(obj);

        public bool IsSingleObjectSelected<T>([NotNullWhen(true)] out T? obj)
            where T : class
        {
            obj = null;
            if (_selectedObjects.Count != 1)
                return false;

            var _obj = _selectedObjects.First();
            if (_obj is not T casted) return false;
            obj = casted;
            return true;
        }

        public void Redo()
        {
            _undoHandler.Redo();
            DoUpdate();
        }

        public void SelectMany<T>(ICollection<T> objects)
        {
            int countBefore = _selectedObjects.Count;
            _selectedObjects.UnionWith(objects.OfType<object>());

            if (_selectedObjects.Count != countBefore)
                SelectionChanged();
        }

        public void Select(object obj)
        {
            int countBefore = _selectedObjects.Count;
            var activeBefore = _activeObject;
            _activeObject = obj;
            _selectedObjects.Add(obj);

            if (_selectedObjects.Count != countBefore ||
                _activeObject != activeBefore)
                SelectionChanged();
        }

        public void Undo()
        {
            _undoHandler.Undo();
            DoUpdate();
        }

        public void WithSuspendUpdateDo(Action action)
        {
            if (_isSuspendUpdate)
            {
                action.Invoke();
                return;
            }

            List<object> prevSelection = [.. _selectedObjects];
            object? prevActive = _activeObject;

            _isSuspendUpdate = true;
            action.Invoke();
            _isSuspendUpdate = false;

            if (_isRequireSelectionCheck)
            {
                if (prevActive != _activeObject ||
                    prevSelection.Count != _selectedObjects.Count ||
                    !_selectedObjects.SetEquals(prevSelection))
                {
                    SelectionChanged();
                    _isRequireUpdate = true;
                }

                _isRequireSelectionCheck = false;
            }

            if (_isRequireUpdate)
            {
                Update?.Invoke();
                _isRequireUpdate = false;
            }
        }

        private void DoUpdate()
        {
            if (_isSuspendUpdate)
            {
                _isRequireUpdate = true;
                return;
            }
            Update?.Invoke();
        }

        private void SelectionChanged()
        {
            if (_activeObject != null && !_selectedObjects.Contains(_activeObject))
                _activeObject = null;

            if (_isSuspendUpdate)
            {
                _isRequireSelectionCheck = true;
                return;
            }
            SelectionVersion++;
            DoUpdate();
        }

        private int _batchActionNestingLevel = 0;
        private readonly HashSet<object> _selectedObjects = [];
        private object? _activeObject = null;

        private readonly UndoHandler _undoHandler = new();

        private List<IRevertable>? _currentActionBatch;
        private bool _isRequireSelectionCheck = false;
        private bool _isRequireUpdate = false;

        private bool _isSuspendUpdate = false;
    }
}
