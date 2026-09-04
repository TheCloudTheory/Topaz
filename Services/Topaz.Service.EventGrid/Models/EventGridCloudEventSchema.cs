namespace Topaz.Service.EventGrid.Models;

internal sealed class EventGridCloudEventSchema
{
    public required string Id { get; set; }          
    public required string Source { get; set; }      
    public required string Type { get; set; }     
    public string SpecVersion { get; set; } = "1.0";     
    public string? DataContentType { get; set; }          
    public string? DataSchema { get; set; }                
    public string? Subject { get; set; }                  
    public DateTimeOffset? Time { get; set; }              
    public object? Data { get; set; }                      
    public string? DataBase64 { get; set; }               
    public IDictionary<string, object>? ExtensionAttributes { get; set; }
}