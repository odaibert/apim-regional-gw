using System.Threading.Tasks;

namespace Common.Interfaces
{
    public interface IBlobStorageConnector<T>
    {
        Task<T> LoadBlob(string equipmentId, string date);
        Task<T[]> LoadBlob(string equipmentId, string startDate, string endDate);
    }
}