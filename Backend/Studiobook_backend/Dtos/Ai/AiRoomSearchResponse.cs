namespace Studiobook_backend.Dtos.Ai
{
    public class AiRoomSearchResponse
    {
        public string Query { get; set; } = string.Empty;

        public AiRoomSearchConditionsDto InterpretedConditions { get; set; } = new();

        public List<AiRoomSearchResultDto> Rooms { get; set; } = new();
    }
}