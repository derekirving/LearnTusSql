#if NET

using System;
using FluentValidation;
using OneOf;

namespace Unify.Extensions;
public static class ResultExtensions
{

    public static T HappyPath<T>(this OneOfBase<T, Exception> result, Action<T> action, ConsoleColor consoleColor = ConsoleColor.Red)
    {
        return result.Match<T>(obj =>
        {
            action(obj);
            return obj;
        }, exception =>
        {
            if (exception is not ValidationException error)
            {
                throw exception;
            }

            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = consoleColor;
            
            foreach (var err in error.Errors)
            {
                Console.WriteLine($"Error! {err.ErrorMessage}");
            }

            Console.ForegroundColor = currentColor;
            
            return default!;
        });
    }
}

#endif