import { useQuery } from '@tanstack/react-query';
import { diagnosticsApi, type ServiceOutboxData } from '../api/diagnostics';

export default function OutboxDump() {
  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['outbox-dump'],
    queryFn: diagnosticsApi.getAllOutboxData,
    refetchInterval: 10000, // Auto-refresh every 10 seconds
  });

  if (isLoading) {
    return (
      <div className="p-6">
        <h2 className="text-2xl font-bold mb-4">Outbox Dump</h2>
        <div className="text-gray-600">Loading outbox data...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-6">
        <h2 className="text-2xl font-bold mb-4">Outbox Dump</h2>
        <div className="text-red-600">Error loading outbox data: {String(error)}</div>
      </div>
    );
  }

  const totalMessages = data?.reduce((sum, service) => sum + service.stats.total, 0) || 0;
  const totalPublished = data?.reduce((sum, service) => sum + service.stats.published, 0) || 0;
  const totalPending = data?.reduce((sum, service) => sum + service.stats.pending, 0) || 0;

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold">Outbox Dump</h2>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
        >
          Refresh
        </button>
      </div>

      {/* Summary Stats */}
      <div className="grid grid-cols-3 gap-4 mb-6">
        <div className="bg-blue-100 p-4 rounded">
          <div className="text-sm text-gray-600">Total Messages</div>
          <div className="text-3xl font-bold">{totalMessages}</div>
        </div>
        <div className="bg-green-100 p-4 rounded">
          <div className="text-sm text-gray-600">Published</div>
          <div className="text-3xl font-bold">{totalPublished}</div>
        </div>
        <div className="bg-yellow-100 p-4 rounded">
          <div className="text-sm text-gray-600">Pending</div>
          <div className="text-3xl font-bold">{totalPending}</div>
        </div>
      </div>

      {/* Per-Service Tables */}
      {data?.map((service) => (
        <ServiceOutboxTable key={service.serviceName} service={service} />
      ))}
    </div>
  );
}

function ServiceOutboxTable({ service }: { service: ServiceOutboxData }) {
  if (service.error) {
    return (
      <div className="mb-8">
        <h3 className="text-xl font-semibold mb-2">{service.serviceName}</h3>
        <div className="text-red-600 text-sm">Error: {service.error}</div>
      </div>
    );
  }

  const formatDate = (dateStr: string) => {
    if (!dateStr) return '-';
    // Ensure date is treated as UTC if no timezone info is present
    const isIsoWithTimeZone = /Z|[+-]\d{2}:\d{2}$/.test(dateStr);
    const utcDateStr = isIsoWithTimeZone ? dateStr : `${dateStr}Z`;
    return new Date(utcDateStr).toLocaleString();
  };

  return (
    <div className="mb-8">
      <div className="flex justify-between items-center mb-2">
        <h3 className="text-xl font-semibold">{service.serviceName}</h3>
        <div className="text-sm text-gray-600">
          Total: {service.stats.total} | Published: {service.stats.published} | Pending:{' '}
          {service.stats.pending}
        </div>
      </div>

      {service.messages.length === 0 ? (
        <div className="text-gray-500 italic text-sm mb-4">(No messages)</div>
      ) : (
        <div className="overflow-x-auto mb-4">
          <table className="min-w-full bg-white border border-gray-300 text-sm">
            <thead className="bg-gray-100">
              <tr>
                <th className="px-4 py-2 border-b text-left">ID</th>
                <th className="px-4 py-2 border-b text-left">Event ID</th>
                <th className="px-4 py-2 border-b text-left">Event Type</th>
                <th className="px-4 py-2 border-b text-left">Created At (Client Time)</th>
                <th className="px-4 py-2 border-b text-center">Published</th>
                <th className="px-4 py-2 border-b text-left">Published At (Client Time)</th>
                <th className="px-4 py-2 border-b text-left">Payload Preview</th>
              </tr>
            </thead>
            <tbody>
              {service.messages.map((msg) => (
                <tr key={msg.id} className="hover:bg-gray-50">
                  <td className="px-4 py-2 border-b">{msg.id}</td>
                  <td className="px-4 py-2 border-b font-mono text-xs">
                    {msg.eventId.substring(0, 8)}...
                  </td>
                  <td className="px-4 py-2 border-b font-semibold">{msg.eventType}</td>
                  <td className="px-4 py-2 border-b">
                    {formatDate(msg.createdAt)}
                  </td>
                  <td className="px-4 py-2 border-b text-center">
                    {msg.published ? (
                      <span className="inline-block px-2 py-1 text-xs bg-green-200 text-green-800 rounded">
                        ✓
                      </span>
                    ) : (
                      <span className="inline-block px-2 py-1 text-xs bg-red-200 text-red-800 rounded">
                        ✗
                      </span>
                    )}
                  </td>
                  <td className="px-4 py-2 border-b">
                    {msg.publishedAt ? formatDate(msg.publishedAt) : '-'}
                  </td>
                  <td className="px-4 py-2 border-b font-mono text-xs truncate max-w-xs">
                    {msg.payloadPreview.substring(0, 80)}...
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
