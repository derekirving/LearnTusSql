#if NET
using System;
using Microsoft.AspNetCore.Mvc;

namespace Unify.Exceptions;

public class ProblemDetailsException(ProblemDetails problemDetails) : Exception(problemDetails.Title)
{
    public ProblemDetails ProblemDetails { get; } = problemDetails;
}
#endif