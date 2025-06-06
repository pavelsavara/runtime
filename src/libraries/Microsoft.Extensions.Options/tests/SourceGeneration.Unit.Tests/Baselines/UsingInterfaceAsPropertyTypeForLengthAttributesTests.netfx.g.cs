
    // <auto-generated/>
    #nullable enable
    #pragma warning disable CS1591 // Compensate for https://github.com/dotnet/roslyn/issues/54103
    namespace Test
{
    partial class MyOptionsValidator
    {
        /// <summary>
        /// Validates a specific named options instance (or all when <paramref name="name"/> is <see langword="null" />).
        /// </summary>
        /// <param name="name">The name of the options instance being validated.</param>
        /// <param name="options">The options instance.</param>
        /// <returns>Validation result.</returns>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Options.SourceGeneration", "42.42.42.42")]
        public global::Microsoft.Extensions.Options.ValidateOptionsResult Validate(string? name, global::Test.MyOptions options)
        {
            global::Microsoft.Extensions.Options.ValidateOptionsResultBuilder? builder = null;
            #if NET10_0_OR_GREATER
            string displayName = string.IsNullOrEmpty(name) ? "MyOptions.Validate" : $"{name}.Validate";
            var context = new global::System.ComponentModel.DataAnnotations.ValidationContext(options, displayName, null, null);
            #else
            var context = new global::System.ComponentModel.DataAnnotations.ValidationContext(options);
            #endif
            var validationResults = new global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult>();
            var validationAttributes = new global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationAttribute>(1);

            context.MemberName = "P2";
            context.DisplayName = string.IsNullOrEmpty(name) ? "MyOptions.P2" : $"{name}.P2";
            validationAttributes.Add(global::__OptionValidationStaticInstances.__Attributes.A1);
            if (!global::System.ComponentModel.DataAnnotations.Validator.TryValidateValue(options.P2, context, validationResults, validationAttributes))
            {
                (builder ??= new()).AddResults(validationResults);
            }

            context.MemberName = "P3";
            context.DisplayName = string.IsNullOrEmpty(name) ? "MyOptions.P3" : $"{name}.P3";
            validationResults.Clear();
            validationAttributes.Clear();
            validationAttributes.Add(global::__OptionValidationStaticInstances.__Attributes.A2);
            if (!global::System.ComponentModel.DataAnnotations.Validator.TryValidateValue(options.P3, context, validationResults, validationAttributes))
            {
                (builder ??= new()).AddResults(validationResults);
            }

            context.MemberName = "P5";
            context.DisplayName = string.IsNullOrEmpty(name) ? "MyOptions.P5" : $"{name}.P5";
            validationResults.Clear();
            validationAttributes.Clear();
            validationAttributes.Add(global::__OptionValidationStaticInstances.__Attributes.A1);
            if (!global::System.ComponentModel.DataAnnotations.Validator.TryValidateValue(options.P5, context, validationResults, validationAttributes))
            {
                (builder ??= new()).AddResults(validationResults);
            }

            context.MemberName = "P6";
            context.DisplayName = string.IsNullOrEmpty(name) ? "MyOptions.P6" : $"{name}.P6";
            validationResults.Clear();
            validationAttributes.Clear();
            validationAttributes.Add(global::__OptionValidationStaticInstances.__Attributes.A2);
            if (!global::System.ComponentModel.DataAnnotations.Validator.TryValidateValue(options.P6, context, validationResults, validationAttributes))
            {
                (builder ??= new()).AddResults(validationResults);
            }

            return builder is null ? global::Microsoft.Extensions.Options.ValidateOptionsResult.Success : builder.Build();
        }
    }
}
namespace __OptionValidationStaticInstances
{
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Options.SourceGeneration", "42.42.42.42")]
    file static class __Attributes
    {
        internal static readonly __OptionValidationGeneratedAttributes.__SourceGen__MinLengthAttribute A1 = new __OptionValidationGeneratedAttributes.__SourceGen__MinLengthAttribute(
            (int)4);

        internal static readonly __OptionValidationGeneratedAttributes.__SourceGen__MaxLengthAttribute A2 = new __OptionValidationGeneratedAttributes.__SourceGen__MaxLengthAttribute(
            (int)5);
    }
}
namespace __OptionValidationStaticInstances
{
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Options.SourceGeneration", "42.42.42.42")]
    file static class __Validators
    {
    }
}
namespace __OptionValidationGeneratedAttributes
{
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Options.SourceGeneration", "42.42.42.42")]
    [global::System.AttributeUsage(global::System.AttributeTargets.Property | global::System.AttributeTargets.Field | global::System.AttributeTargets.Parameter, AllowMultiple = false)]
    file class __SourceGen__MaxLengthAttribute : global::System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        private const int MaxAllowableLength = -1;
        private static string DefaultErrorMessageString => "The field {0} must be a string or array type with a maximum length of '{1}'.";
        public __SourceGen__MaxLengthAttribute(int length) : base(() => DefaultErrorMessageString) { Length = length; }
        public __SourceGen__MaxLengthAttribute(): base(() => DefaultErrorMessageString) { Length = MaxAllowableLength; }
        public int Length { get; }
        public override string FormatErrorMessage(string name) => string.Format(global::System.Globalization.CultureInfo.CurrentCulture, ErrorMessageString, name, Length);
        public override bool IsValid(object? value)
        {
            if (Length == 0 || Length < -1)
            {
                throw new global::System.InvalidOperationException("MaxLengthAttribute must have a Length value that is greater than zero. Use MaxLength() without parameters to indicate that the string or array can have the maximum allowable length.");
            }
            if (value == null || MaxAllowableLength == Length)
            {
                return true;
            }

            int length;
            if (value is string stringValue)
            {
                length = stringValue.Length;
            }
            else if (value is System.Collections.ICollection collectionValue)
            {
                length = collectionValue.Count;
            }
            else if (value is global::System.Collections.Generic.IList<string>)
            {
                length = ((global::System.Collections.Generic.IList<string>)value).Count;
            }
            else if (value is global::System.Collections.Generic.ICollection<string>)
            {
                length = ((global::System.Collections.Generic.ICollection<string>)value).Count;
            }
            else
            {
                throw new global::System.InvalidCastException($"The field of type {value.GetType()} must be a string, array, or ICollection type.");
            }

            return length <= Length;
        }
    }
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Options.SourceGeneration", "42.42.42.42")]
    [global::System.AttributeUsage(global::System.AttributeTargets.Property | global::System.AttributeTargets.Field | global::System.AttributeTargets.Parameter, AllowMultiple = false)]
    file class __SourceGen__MinLengthAttribute : global::System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        private static string DefaultErrorMessageString => "The field {0} must be a string or array type with a minimum length of '{1}'.";

        public __SourceGen__MinLengthAttribute(int length) : base(() => DefaultErrorMessageString) { Length = length; }
        public int Length { get; }
        public override bool IsValid(object? value)
        {
            if (Length < -1)
            {
                throw new global::System.InvalidOperationException("MinLengthAttribute must have a Length value that is zero or greater.");
            }
            if (value == null)
            {
                return true;
            }

            int length;
            if (value is string stringValue)
            {
                length = stringValue.Length;
            }
            else if (value is System.Collections.ICollection collectionValue)
            {
                length = collectionValue.Count;
            }
            else if (value is global::System.Collections.Generic.IList<string>)
            {
                length = ((global::System.Collections.Generic.IList<string>)value).Count;
            }
            else if (value is global::System.Collections.Generic.ICollection<string>)
            {
                length = ((global::System.Collections.Generic.ICollection<string>)value).Count;
            }
            else
            {
                throw new global::System.InvalidCastException($"The field of type {value.GetType()} must be a string, array, or ICollection type.");
            }

            return length >= Length;
        }
        public override string FormatErrorMessage(string name) => string.Format(global::System.Globalization.CultureInfo.CurrentCulture, ErrorMessageString, name, Length);
    }
}
