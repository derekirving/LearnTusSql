using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Unify.Validation
{
    public class MustBeCheckedAttribute : ValidationAttribute, IClientModelValidator
    {
        private const string DefaultErrorMessage =
            "You must tick this";

        public void AddValidation(ClientModelValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-mustbechecked", ErrorMessage ?? DefaultErrorMessage);
        }

        protected override ValidationResult IsValid(object value,
        ValidationContext validationContext)
        {
            if (value is bool && (bool)value)
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(ErrorMessage ?? DefaultErrorMessage);
        }

        public override bool IsValid(object value)
        {
            return value is bool && (bool)value;
        }

        private bool MergeAttribute(IDictionary<string, string> attributes, string key, string value)
        {
            if (attributes.ContainsKey(key))
            {
                return false;
            }

            attributes.Add(key, value);
            return true;
        }
    }
}
