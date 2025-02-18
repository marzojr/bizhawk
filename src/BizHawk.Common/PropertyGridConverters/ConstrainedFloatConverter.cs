using System.ComponentModel;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Linq;

namespace BizHawk.Common
{
	/// <summary>
	/// Used in conjunction with the <see cref="RangeAttribute" /> will perform range validation against a float value using PropertyGrid
	/// </summary>
	public class ConstrainedFloatConverter : TypeConverter
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

			if (float.TryParse(value.ToString(), out var floatVal))
			{
				return floatVal;
			}

			throw new FormatException($"Invalid value: {value}, {context.PropertyDescriptor.Name} must be a float.");
		}

		public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
		{
			if (destinationType == null)
			{
				throw new ArgumentNullException(nameof(destinationType));
			}

			if (destinationType == typeof(string))
			{
				var num = Convert.ToSingle(value);
				return num.ToString(NumberFormatInfo.InvariantInfo);
			}

			return base.ConvertTo(context, culture, value, destinationType)!;
		}
	}
}
