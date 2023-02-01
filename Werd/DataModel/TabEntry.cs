using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Werd.DataModel
{
	public class TabEntry
	{
		public string Type { get; private set; }
		public string Location { get; set; }
		public Guid Id { get; private set; }

		public TabEntry(string tabType, Guid id) : this(tabType, id, string.Empty) { }

		[JsonConstructor]
		public TabEntry(string type, Guid id, string location)
		{
			Id = id;
			Location = location;
			this.Type = type;
		}

		public override string ToString() { return $"Type: {Type}, Id: {Id}, Location: {Location}"; }
	}
}
