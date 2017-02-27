/* This source code licensed under the GNU Affero General Public License */
using System;

namespace Highpoint.Sage.Utility
{
    public class FieldDescriptionAttribute : Attribute
    {
        public FieldDescriptionAttribute(string v)
        {
            this.Value = v;
        }

        public string Value { get; }
    }
}