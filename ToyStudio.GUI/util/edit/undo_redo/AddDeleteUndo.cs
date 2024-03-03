﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.GUI.util.edit.undo_redo
{
    internal static class ListUndoExtensions
    {
        public static IRevertable RevertableAdd<T>(this IList<T> list, T item, string actionName = "Unnamed Action")
        {
            list.Add(item);
            return new RevertableInsertIntoList<T>(list, list.Count - 1, actionName);
        }

        public static IRevertable RevertableInsert<T>(this IList<T> list, T item, int index, string actionName = "Unnamed Action")
        {
            list.Insert(index, item);
            return new RevertableInsertIntoList<T>(list, index, actionName);
        }

        public static IRevertable RevertableRemove<T>(this IList<T> list, T item, string actionName = "Unnamed Action")
        {
            int index = list.IndexOf(item);
            return list.RevertableRemoveAt(index, actionName);
        }

        public static IRevertable RevertableRemoveAt<T>(this IList<T> list, int index, string actionName = "Unnamed Action")
        {
            var item = list[index];
            list.RemoveAt(index);
            return new RevertableRemoveFromList<T>(list, item, index, actionName);
        }
    }

    internal class RevertableInsertIntoList<T>(IList<T> list, int index, string name) : IRevertable
    {
        public string Name => name;

        public IRevertable Revert() => list.RevertableRemoveAt(index);
    }

    internal class RevertableRemoveFromList<T>(IList<T> list, T item, int index, string name) : IRevertable
    {
        public string Name => name;

        public IRevertable Revert() => list.RevertableInsert(item, index);
    }
}
