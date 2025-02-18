using System.ComponentModel;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Linq;

namespace BizHawk.Common
{
	/// <summary>
	/// Used in conjunction with the <see cref="RangeAttribute" /> will perform range validation against an int value using PropertyGrid
	/// </summary>
	public class ConstrainedIntConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
		{
			return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		}

		public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
		{
			var range = (RangeAttribute)context!.Instance
				.GetType()
				.GetProperty(context.PropertyDescriptor!.Name)!
				.GetCustomAttributes()
				.First(i => i.GetType() == typeof(RangeAttribute));

			range.Validate(value, context.PropertyDescriptor.Name);

			if (value == null)
			{
				throw new FormatException($"{context.PropertyDescriptor.Name} can not be null");
			}

			if (int.TryParse(value.ToString(), out var intVal))
			{
				return intVal;
			}

			throw new FormatException($"Invalid value: {value}, {context.PropertyDescriptor.Name} must be an integer.");
		}

		public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
		{
			if (destinationType == null)
			{
				throw new ArgumentNullException(nameof(destinationType));
			}

			if (destinationType == typeof(string))
			{
				var num = Convert.ToInt32(value);
				return num.ToString();
			}

			return base.ConvertTo(context, culture, value, destinationType)!;
		}
	}
}
