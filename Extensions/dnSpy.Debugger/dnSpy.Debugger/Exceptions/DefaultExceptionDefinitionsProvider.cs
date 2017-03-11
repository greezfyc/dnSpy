﻿/*
    Copyright (C) 2014-2017 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger.Exceptions;

namespace dnSpy.Debugger.Exceptions {
	[Export(typeof(DefaultExceptionDefinitionsProvider))]
	sealed class DefaultExceptionDefinitionsProvider {
		public DbgExceptionGroupDefinition[] GroupDefinitions { get; }
		public DbgExceptionDefinition[] Definitions { get; }

		[ImportingConstructor]
		DefaultExceptionDefinitionsProvider([ImportMany] IEnumerable<Lazy<DbgExceptionDefinitionProvider, IDbgExceptionDefinitionProviderMetadata>> dbgExceptionDefinitionProviders) {
			var providers = dbgExceptionDefinitionProviders.OrderBy(a => a.Metadata.Order).ToArray();

			var groupDefs = new Dictionary<string, DbgExceptionGroupDefinition>(StringComparer.Ordinal);
			foreach (var p in providers) {
				foreach (var def in p.Value.CreateGroups()) {
					if (!groupDefs.ContainsKey(def.Name))
						groupDefs.Add(def.Name, def);
				}
			}
			GroupDefinitions = groupDefs.Select(a => a.Value).ToArray();

			var defs = new Dictionary<DbgExceptionId, DbgExceptionDefinition>();
			foreach (var p in providers) {
				foreach (var def in p.Value.Create()) {
					bool b = groupDefs.ContainsKey(def.Id.Group);
					Debug.Assert(b);
					if (!b)
						continue;
					if (!defs.ContainsKey(def.Id))
						defs.Add(def.Id, def);
				}
			}
			foreach (var group in GroupDefinitions) {
				var id = new DbgExceptionId(group.Name);
				if (!defs.ContainsKey(id))
					defs.Add(id, new DbgExceptionDefinition(id, DbgExceptionDefinitionFlags.None));
			}
			Definitions = defs.Select(a => a.Value).ToArray();
		}
	}
}
