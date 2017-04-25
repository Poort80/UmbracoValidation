using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using System.Web.Configuration;
using System.Web.Mvc;
using Poort80.Umbraco.Validation.Infrastructure;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Web;

namespace Poort80.Umbraco.Validation
{
    public class UmbracoValidation : DataAnnotationsModelMetadataProvider
    {
        protected override ModelMetadata CreateMetadata(IEnumerable<Attribute> attributes, Type containerType, Func<object> modelAccessor, Type modelType, string propertyName)
        {
            if (containerType == null || Attribute.GetCustomAttribute(containerType, typeof(UmbracoModelMetadataAttribute)) == null || Attribute.GetCustomAttribute(containerType.GetProperty(propertyName), typeof(NoMetadataAttribute)) != null)
            {
                //default metadate provider
                return base.CreateMetadata(attributes, containerType, modelAccessor, modelType, propertyName);
            }

            var containerAttribute = (UmbracoModelMetadataAttribute)Attribute.GetCustomAttribute(containerType, typeof(UmbracoModelMetadataAttribute));

            if (UmbracoContext.Current == null)
                return base.CreateMetadata(attributes, containerType, modelAccessor, modelType, propertyName);

            var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
            var doc = umbracoHelper.AssignedContentItem;
            if (doc == null)
                return base.CreateMetadata(attributes, containerType, modelAccessor, modelType, propertyName);

            string documentType = !string.IsNullOrEmpty(containerAttribute.DocumentTypeAlias) ? containerAttribute.DocumentTypeAlias : doc.DocumentTypeAlias;

            //meta date provider using labels
            var containerPrefix = containerType.Name.Replace("ViewModel", string.Empty);
            var newAttributes = attributes.ToList();

            if (containerAttribute.Mode.HasFlag(UmbracoValidationMode.Validation))
            {
                foreach (var attribute in newAttributes)
                {
                    var validationAttribute = attribute as ValidationAttribute;
                    if (validationAttribute != null)
                    {
                        var propertyAlias = GetValidationPropertyAlias(validationAttribute, propertyName);
                        CreateMissingValidationDocumentTypeProperty(documentType, GetValidationPropertyName(validationAttribute, propertyName), propertyAlias);

                        var umbracoProperty = doc.GetProperty(propertyAlias);
                        var errorMessage = umbracoProperty?.GetValue<string>();
                        if (!string.IsNullOrEmpty(errorMessage))
                            validationAttribute.ErrorMessage = errorMessage;
                    }
                }
            }

            var data = base.CreateMetadata(newAttributes, containerType, modelAccessor, modelType, propertyName);

            if (containerAttribute.Mode.HasFlag(UmbracoValidationMode.Name))
            {
                var propertyAlias = GetLabelForPropertyAlias(propertyName);
                CreateMissingValidationDisplayTypeProperty(documentType, propertyName, propertyAlias);

                var umbracoProperty = doc.GetProperty(propertyAlias);
                if (umbracoProperty != null)
                {
                    data.DisplayName = umbracoProperty.GetValue<string>();
                }
            }

            return data;
        }

        private void CreateMissingValidationDisplayTypeProperty(string documentTypeAlias, string propertyName, string propertyAlias)
        {
            if (IsInDebugMode())
            {
                var tabName = "Form labels";

                var service = ApplicationContext.Current.Services.ContentTypeService;
                var docType = service.GetContentType(documentTypeAlias);

                if (docType.PropertyGroups.All(a => a.Name != tabName))
                {
                    docType.AddPropertyGroup(tabName);
                    service.Save(docType);
                }

                if (docType.PropertyTypes.All(a => a.Alias != propertyAlias))
                {
                    var dataTypeService = ApplicationContext.Current.Services.DataTypeService;
                    IDataTypeDefinition textBox = dataTypeService.GetAllDataTypeDefinitions(-88).First();

                    docType.AddPropertyType(new PropertyType(textBox) { Alias = propertyAlias, Name = propertyName }, tabName);
                    service.Save(docType);
                }
            }
        }

        private void CreateMissingValidationDocumentTypeProperty(string documentTypeAlias, string propertyName, string propertyAlias)
        {
            if (IsInDebugMode())
            {
                var service = ApplicationContext.Current.Services.ContentTypeService;
                var docType = service.GetContentType(documentTypeAlias);

                if (docType.PropertyGroups.All(a => a.Name != "Validation"))
                {
                    docType.AddPropertyGroup("Validation");
                    service.Save(docType);
                }

                if (docType.PropertyTypes.All(a => a.Alias != propertyAlias))
                {
                    var dataTypeService = ApplicationContext.Current.Services.DataTypeService;
                    IDataTypeDefinition textBox = dataTypeService.GetAllDataTypeDefinitions(-88).First();

                    docType.AddPropertyType(new PropertyType(textBox) { Alias = propertyAlias, Name = propertyName }, "Validation");
                    service.Save(docType);
                }
            }
        }

        private bool IsInDebugMode()
        {
            var configSection = (CompilationSection)ConfigurationManager.GetSection("system.web/compilation");
            return configSection.Debug;
        }

        private string GetValidationPropertyAlias(ValidationAttribute attribute, string propertyName)
        {
            const string prefix = "validation";
            if (attribute is RegularExpressionAttribute || attribute is EmailAddressAttribute)
            {
                return string.Concat(prefix, "InvalidFormat", propertyName);
            }
            return string.Concat(prefix, attribute.GetType().Name.Replace("Attribute", string.Empty), propertyName);
        }

        private string GetLabelForPropertyAlias(string propertyName)
        {
            const string prefix = "labelFor";
            return string.Concat(prefix, propertyName);
        }

        private string GetValidationPropertyName(ValidationAttribute attribute, string propertyName)
        {
            propertyName = propertyName.UppercaseFirst();
            if (attribute is RegularExpressionAttribute || attribute is EmailAddressAttribute)
            {
                return "InvalidFormat " + propertyName;
            }

            return string.Concat(attribute.GetType().Name.Replace("Attribute", string.Empty), " ", propertyName);
        }
    }

    public class UmbracoModelMetadataAttribute : Attribute
    {
        public UmbracoModelMetadataAttribute(UmbracoValidationMode mode = UmbracoValidationMode.Validation)
        {
            Mode = mode;
        }

        public string DocumentTypeAlias { get; set; }

        public UmbracoValidationMode Mode { get; set; }

    }

    public class NoMetadataAttribute : Attribute
    {
    }
}
