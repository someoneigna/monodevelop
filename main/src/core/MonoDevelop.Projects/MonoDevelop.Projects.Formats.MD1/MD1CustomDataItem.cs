// MD1CustomDataItem.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Projects.Formats.MD1
{
	class MD1CustomDataItem: ICustomDataItemHandler
	{
		public DataCollection Serialize (object obj, ITypeSerializer handler)
		{
			if (obj is SolutionEntityItem) {
				if (obj is DotNetProject) {
					DotNetProject project = obj as DotNetProject;
					foreach (DotNetProjectConfiguration config in project.Configurations)
						config.ExtendedProperties ["Build/target"] = project.CompileTarget.ToString ();
				}
				DataCollection data = handler.Serialize (obj);
				SolutionEntityItem item = (SolutionEntityItem) obj;
				if (item.DefaultConfiguration != null) {
					DataItem confItem = data ["Configurations"] as DataItem;
					if (confItem != null) {
						confItem.UniqueNames = true;
						if (item.ParentSolution != null)
							confItem.ItemData.Add (new DataValue ("active", item.ParentSolution.DefaultConfigurationId));
					}
				}
				return data;
			}
			else if (obj is ProjectReference) {
				ProjectReference pref = (ProjectReference) obj;
				DataCollection data = handler.Serialize (obj);
				string refto = pref.Reference;
				if (pref.ReferenceType == ReferenceType.Assembly) {
					string basePath = Path.GetDirectoryName (handler.SerializationContext.BaseFile);
					refto = FileService.AbsoluteToRelativePath (basePath, refto);
				} else if (pref.ReferenceType == ReferenceType.Gac && pref.LoadedReference != null)
					refto = pref.LoadedReference;
	
				data.Add (new DataValue ("refto", refto));
				return data;
			}
			return handler.Serialize (obj);
		}
		
		public void Deserialize (object obj, ITypeSerializer handler, DataCollection data)
		{
			if (obj is SolutionEntityItem) {
				DataValue ac = null;
				DataItem confItem = data ["Configurations"] as DataItem;
				if (confItem != null)
					ac = (DataValue) confItem.ItemData.Extract ("active");
					
				handler.Deserialize (obj, data);
				if (ac != null) {
					SolutionEntityItem item = (SolutionEntityItem) obj;
					item.DefaultConfigurationId = ac.Value;
				}
				DotNetProject np = obj as DotNetProject;
				if (np != null) {
					// Get the compile target from one of the configurations
					if (np.Configurations.Count > 0)
						np.CompileTarget = (CompileTarget) Enum.Parse (typeof(CompileTarget), (string) np.Configurations [0].ExtendedProperties ["Build/target"]);
				}
			}
			else if (obj is ProjectReference) {
				ProjectReference pref = (ProjectReference) obj;
				DataValue refto = data.Extract ("refto") as DataValue;
				handler.Deserialize (obj, data);
				if (refto != null) {
					pref.Reference = refto.Value;
					if (pref.ReferenceType == ReferenceType.Assembly) {
						string basePath = Path.GetDirectoryName (handler.SerializationContext.BaseFile);
						pref.Reference = FileService.RelativeToAbsolutePath (basePath, pref.Reference);
					}
				}
			} else
				handler.Deserialize (obj, data);
		}
	}
}
