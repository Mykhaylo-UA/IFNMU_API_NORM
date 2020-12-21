using System;
using System.Text.Json.Serialization;

namespace IFNMU_API_NORM.Abstract
{
    public abstract class BaseModel
    {
        public Guid Id { get; set; }
    }
}