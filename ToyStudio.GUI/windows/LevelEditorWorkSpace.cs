using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core;
using ToyStudio.GUI.common.gl;
using ToyStudio.GUI.common.modal;

namespace ToyStudio.GUI.windows
{
    internal class LevelEditorWorkSpace
    {
        private Level _level;
        private GLTaskScheduler _glScheduler;
        private IPopupModalHost _popupModalHost;

        public static async Task<LevelEditorWorkSpace> Create(Level level,
            GLTaskScheduler glScheduler,
            IPopupModalHost popupModalHost,
            IProgress<(string operationName, float? progress)> progress)
        {
            var cs = new LevelEditorWorkSpace(level, glScheduler, popupModalHost);

            //do gl resource loading here

            return cs;
        }

        //for now
        public bool HasUnsavedChanges() => false;
        public void Save() { }

        public void Undo() { }
        public void Redo() { }
        public void PreventFurtherRendering() { }

        public void DrawUI(GL gl, double deltaSeconds)
        {

        }

        private LevelEditorWorkSpace(Level level, GLTaskScheduler glScheduler, IPopupModalHost popupModalHost)
        {
            _level = level;
            _glScheduler = glScheduler;
            _popupModalHost = popupModalHost;
        }
    }
}
