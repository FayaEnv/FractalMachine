﻿/*
   Copyright 2020 (c) Riccardo Cecchini
   
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using FractalMachine.Classes;
using FractalMachine.Code.Components;
using FractalMachine.Code.Langs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FractalMachine.Code
{
    abstract public class Component
    {
        internal string name;
        internal Type returnType;
        internal Types type;
        internal Linear _linear;
        internal Component parent;

        public Dictionary<string, Component> components = new Dictionary<string, Component>();
        internal Dictionary<string, string> parameters = new Dictionary<string, string>();

        public Component(Component parent, Linear Linear) 
        {
            this.parent = parent;
            _linear = Linear;

            if (_linear != null)
            {
                _linear.component = this;
                ReadLinear();
            }
        }

        #region ReadLinear

        public virtual void ReadLinear() { } 

        #endregion

        #region ComponentTypes

        public enum Types
        {
            Container,
            Function,
            Member,
            Operation
        }

        #endregion

        #region AddComponents

        internal Component getBaseComponent(string Name, out string toCreate)
        {
            var names = Name.Split('.').ToList();
            toCreate = names.Pull();

            Component baseComp = this;
            if (names.Count > 0)
                baseComp = Solve(String.Join(".", names));

            return baseComp;
        } 

        internal void addComponent(string Name, Component comp)
        {
            string toCreate;
            var baseComp = getBaseComponent(Name, out toCreate);

            baseComp.components[toCreate] = comp;
            comp.name = toCreate;
        }

        internal Component getComponent(string Name)
        {
            string toCreate;
            var baseComp = getBaseComponent(Name, out toCreate);
            Component comp;
            baseComp.components.TryGetValue(Name, out comp);
            return comp;
        }

        #endregion

        #region SubComponents

        public Component Solve(string Name, bool DontPanic = false)
        {
            var parts = Name.Split('.');
            return Solve(parts, DontPanic);
        }

        public Component Solve(string[] Names, bool DontPanic = false)
        {
            Component comp = this, bcomp = this;
            var tot = "";

            if (Names.Length == 1)
            {
                // check unique call
                var name = Names[0];
                if (name.StartsWith(Properties.NativeFunctionPrefix))
                {
                    // Is C function
                    if (name.StartsWith(Properties.NativeFunctionPrefix+"c_"))
                    {
                        name = name.Substring(5);
                        var spl = name.Split('_');
                        var lib = spl[0];
                        name = spl[1];

                        TopFile.IncludeDefault(lib);

                        //Create dummy component function
                        var fun = new Function(null, null);
                        fun.name = name;
                        return fun;
                    }
                }
            }

            while (!comp.components.TryGetValue(Names[0], out comp))
            {
                comp = bcomp.parent;
                if (comp == null)
                {
                    if (!DontPanic) throw new Exception("Error, " + Names[0] + " not found");
                    return null;
                }
                bcomp = comp;
            }

            for (int p = 1; p < Names.Length; p++)
            {
                if(comp is Components.File)
                {
                    ((Components.File)comp).Load();
                }

                var part = Names[p];
                if (!comp.components.TryGetValue(part, out comp))
                {
                    if (!DontPanic) throw new Exception("Error, " + tot + part + " not found");
                    return null;
                }

                tot += part + ".";
            }

            return comp;
        }

        #endregion

        #region Properties

        public Component Top
        {
            get
            {
                return (parent!=null && parent != this) ? parent.Top : this;
            }
        }

        public virtual Components.File TopFile
        {
            get
            {
                return parent.TopFile;
            }
        }

        public virtual Project GetProject
        {
            get
            {
                return parent.GetProject;
            }
        }

        #endregion

        #region Methods

        public string GetName(Component relativeTo = null)
        {
            if (relativeTo == this)
                return null;

            string topName = null;

            /*
                This is a delicate part. The type should be specified if part of a static class or namespace
                The code below, pratically, is not working for the moment
            */
            if ((parent is Components.File && TopFile != parent) || parent is Components.Class)
                topName = parent?.GetName(relativeTo);

            return (topName != null ? topName + '.' : "") + name;
        }

        #endregion

        #region Called

        internal bool _called = false;

        public bool Called
        {
            get
            {
                if (!(this is Components.Container))
                    return true;

                if (_called) return true;
                foreach (var comp in components)
                {
                    if (comp.Value.Called) return true;
                }

                return false;
            }
        }

        public Linear Linear
        {
            get
            {
                return _linear;
            }
        }

        #endregion

        #region Writer

        internal bool written = false;
        internal int writeContLength = 0;
        internal List<string> writeCont = new List<string>();
        internal Component writeRedirectTo;

        abstract public string WriteTo(Lang Lang);

        internal virtual void writeReset()
        {
            writeCont.Clear();
            writeContLength = 0;

            if (written) {
                written = false;
                foreach (var i in components)
                    i.Value.writeReset();
            }
        }

        internal virtual int writeNewLine()
        {
            writeToCont("\r\n");
            return parent.writeNewLine();
        }

        internal void writeToCont(string str)
        {
            writeCont.Add(str);
            writeContLength += str.Length;
        }

        internal string writeReturn()
        {
            var strBuild = new StringBuilder(writeContLength);
            strBuild.AppendJoin("", writeCont.ToArray());

            if(writeRedirectTo != null)
            {
                writeRedirectTo.writeCont.Add(strBuild.ToString());
                return "";
            }

            return strBuild.ToString();
        }
        
        internal void writeRedirect(Component to)
        {
            writeRedirectTo = to;
        }

        #endregion
    }
}
