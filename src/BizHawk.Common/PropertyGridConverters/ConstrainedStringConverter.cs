using System.ComponentModel;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Linq;

namespace BizHawk.Common
{
	/// <summary>
	/// Used in conjunction with the <see cref="MaxLengthAttribute" /> will perform max length validation against a string value using PropertyGrid
	/// </summary>
	public class ConstrainedStringConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
		{
			return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		}

		public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
		{
			var maxLength = (MaxLengthAttribute)context!.Instance
				.GetType()
				.GetProperty(context.PropertyDescriptor!.Name)!
				.GetCustomAttributes()
				.First(i => i.GetType() == typeof(MaxLengthAttribute));

			maxLength.Validate(value, context.PropertyDescriptor.Name);

			if (value == null)
			{
				throw new FormatException($"{context.PropertyDescriptor.Name} can not be null");
			}

			return value.ToString()!;
		}
	}
}
