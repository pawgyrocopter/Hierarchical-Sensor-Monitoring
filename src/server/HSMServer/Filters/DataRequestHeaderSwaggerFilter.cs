﻿using HSMSensorDataObjects;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace HSMServer.Filters
{
    /// <summary>
    /// Swagger filter for adding required string 'Key' in API requests header
    /// </summary>
    public sealed class DataRequestHeaderSwaggerFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Parameters ??= new List<OpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = nameof(BaseRequest.Key),
                In = ParameterLocation.Header,
                Required = true,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                }
            });

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = nameof(BaseRequest.ClientName),
                In = ParameterLocation.Header,
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                }
            });
        }
    }
}
