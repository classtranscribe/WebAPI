using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ClassTranscribeServer.Utils
{
    public class FileUploadOperation : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // TODO: Deprecated code, need to fix feature to allow uploading file via Swagger UI
            //if (operation.OperationId!= null .ToLower() == "apivaluesuploadpost")
            //{
            //    operation.Parameters.Clear();
            //    operation.Parameters.Add(new OpenApiParameter
            //    {
            //        Name = "uploadedFile",
            //        Description = "Upload File",
            //        Required = true
            //    });
            //}
        }
    }
}
