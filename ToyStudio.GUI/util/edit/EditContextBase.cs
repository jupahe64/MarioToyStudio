using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.GUI.util.edit.undo_redo;

namespace ToyStudio.GUI.util.edit
{
    interface ICommittable
    {
        void Commit(string name);
    }

    internal class EditContextBase
    {

        public ulong SelectionVersion { get; private set; } = 0;

        public event Action? Update;

        public ICommittable BeginBatchAction()
        {
            _currentActionBatch = [];
            var batchAction = new BatchAction(this);
            _nestedBatchActions.Push(batchAction);
            return batchAction;
        }

        public void CommitAction(IRevertable action)
        {
            if (_currentActionBatch is not null)
            {
                _currentActionBatch.Add(action);
                return;
            }

            _undoHandler.AddToUndo(action);
            Update?.Invoke();
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
            if (_selectedObjects.Count > 0)
                SelectionChanged();

            _selectedObjects.Clear();
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
            Update?.Invoke();
        }

        public void Select(ICollection<object> objects)
        {
            int countBefore = _selectedObjects.Count;
            _selectedObjects.UnionWith(objects);

            if (_selectedObjects.Count != countBefore)
                SelectionChanged();
        }

        public void Select(object obj)
        {
            int countBefore = _selectedObjects.Count;
            _selectedObjects.Add(obj);

            if (_selectedObjects.Count != countBefore)
                SelectionChanged();
        }

        public void Undo()
        {
            _undoHandler.Undo();
            Update?.Invoke();
        }

        public void WithSuspendUpdateDo(Action action)
        {
            if (_isSuspendUpdate)
            {
                action.Invoke();
                return;
            }

            List<object> prevSelection = _selectedObjects.ToList();

            _isSuspendUpdate = true;
            action.Invoke();
            _isSuspendUpdate = false;

            if (_isRequireSelectionCheck)
            {
                if (prevSelection.Count != _selectedObjects.Count ||
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
        private void EndBatchAction(BatchAction action)
        {
            if (action != _nestedBatchActions.Pop())
                throw new InvalidOperationException($"Nested batch action {action.Name} committed in incorrect order");

            if (_nestedBatchActions.Count > 0)
                //we're still nested
                return;

            if (_currentActionBatch is null || _currentActionBatch.Count == 0)
                return;

            _undoHandler.AddToUndo(_currentActionBatch, action.Name);
            _currentActionBatch = null;
            Update?.Invoke();
        }

        private void SelectionChanged()
        {
            if (_isSuspendUpdate)
            {
                _isRequireSelectionCheck = true;
                return;
            }
            SelectionVersion++;
            Update?.Invoke();
        }

        private readonly Stack<BatchAction> _nestedBatchActions = [];
        private readonly HashSet<object> _selectedObjects = [];

        private readonly UndoHandler _undoHandler = new();

        private List<IRevertable>? _currentActionBatch;
        private bool _isRequireSelectionCheck = false;
        private bool _isRequireUpdate = false;

        private bool _isSuspendUpdate = false;


        private class BatchAction(EditContextBase context) : ICommittable
        {
            public string Name { get; private set; } = "Unfinished Batch Action";
            public void Commit(string name)
            {
                Name = name;
                context.EndBatchAction(this);
            }
        }
    }
}
