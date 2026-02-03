import React, { useState, useEffect } from 'react';
import { diagnosticsApi, OutboxMessage, ServiceBusMessage } from '../api/diagnostics';

type Tab = 'outbox' | 'queue' | 'dlq';

const SBInspector: React.FC = () => {
  const [activeTab, setActiveTab] = useState<Tab>('outbox');
  const [outboxMessages, setOutboxMessages] = useState<OutboxMessage[]>([]);
  const [queueMessages, setQueueMessages] = useState<ServiceBusMessage[]>([]);
  const [dlqMessages, setDlqMessages] = useState<ServiceBusMessage[]>([]);
  
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedPayload, setSelectedPayload] = useState<string | null>(null);

  const fetchData = async () => {
    setLoading(true);
    setError(null);
    try {
      if (activeTab === 'outbox') {
        const data = await diagnosticsApi.getOutboxMessages('');
        setOutboxMessages(data.messages);
      } else if (activeTab === 'queue') {
        const msgs = await diagnosticsApi.getQueueMessages();
        setQueueMessages(msgs);
      } else if (activeTab === 'dlq') {
        const msgs = await diagnosticsApi.getDlqMessages();
        setDlqMessages(msgs);
      }
    } catch (err: any) {
      setError(err.response?.data?.error || err.message || 'Failed to fetch data');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, [activeTab]);

  return (
    <div className="container mx-auto p-6">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold">Service Bus Inspector</h1>
        <button 
          onClick={fetchData}
          disabled={loading}
          className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:bg-blue-300"
        >
          {loading ? 'Refreshing...' : 'Refresh'}
        </button>
      </div>

      {/* Tabs */}
      <div className="flex border-b mb-4">
        <button
          className={`py-2 px-4 ${activeTab === 'outbox' ? 'border-b-2 border-blue-500 font-bold text-blue-600' : 'text-gray-500'}`}
          onClick={() => setActiveTab('outbox')}
        >
          Outbox (Events)
        </button>
        <button
          className={`py-2 px-4 ${activeTab === 'queue' ? 'border-b-2 border-blue-500 font-bold text-blue-600' : 'text-gray-500'}`}
          onClick={() => setActiveTab('queue')}
        >
          Active Command Queue
        </button>
        <button
          className={`py-2 px-4 ${activeTab === 'dlq' ? 'border-b-2 border-blue-500 font-bold text-blue-600' : 'text-gray-500'}`}
          onClick={() => setActiveTab('dlq')}
        >
          Dead Letter Queue
        </button>
      </div>

      {error && (
        <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
          Error: {error}
        </div>
      )}

      {/* Content */}
      <div className="bg-white shadow-md rounded overflow-hidden">
        {activeTab === 'outbox' && (
          <OutboxTable messages={outboxMessages} onViewPayload={setSelectedPayload} />
        )}
        {activeTab === 'queue' && (
          <ServiceBusTable messages={queueMessages} onViewPayload={setSelectedPayload} emptyText="No active commands in queue (they are processed quickly!)" />
        )}
        {activeTab === 'dlq' && (
          <ServiceBusTable messages={dlqMessages} onViewPayload={setSelectedPayload} isDlq={true} emptyText="No messages in Dead Letter Queue (Good!)" />
        )}
      </div>

      {/* Modal for Details */}
      {selectedPayload && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50" onClick={() => setSelectedPayload(null)}>
          <div className="relative top-20 mx-auto p-5 border w-3/4 shadow-lg rounded-md bg-white" onClick={e => e.stopPropagation()}>
            <div className="mt-3">
              <h3 className="text-lg leading-6 font-medium text-gray-900 pb-2 border-b">Message Payload</h3>
              <div className="mt-2 px-7 py-3">
                <pre className="bg-gray-100 p-4 rounded text-sm overflow-auto max-h-96">
                  {selectedPayload}
                </pre>
              </div>
              <div className="items-center px-4 py-3">
                <button
                  className="px-4 py-2 bg-blue-500 text-white text-base font-medium rounded-md w-full shadow-sm hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-300"
                  onClick={() => setSelectedPayload(null)}
                >
                  Close
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

const OutboxTable: React.FC<{ messages: OutboxMessage[], onViewPayload: (p: string) => void }> = ({ messages, onViewPayload }) => {
  const getCausationId = (payload: string) => {
    try { return JSON.parse(payload).causationId || 'N/A'; } catch { return 'Error'; }
  };
  
  return (
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">ID</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Event</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Timestamps</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Action</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {messages.map((msg) => (
              <tr key={msg.id} className="hover:bg-gray-50">
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">{msg.id}</td>
                <td className="px-6 py-4 text-sm text-gray-900">
                  <div className="font-bold text-blue-600">{msg.eventType}</div>
                  <div className="text-xs text-gray-500">Cmd: {getCausationId(msg.payload)}</div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                    msg.published ? 'bg-green-100 text-green-800' : 'bg-yellow-100 text-yellow-800'
                  }`}>
                    {msg.published ? 'Published' : 'Pending'}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  <div>{new Date(msg.createdAt).toLocaleTimeString()}</div>
                </td>
                <td className="px-6 py-4 text-sm"><button onClick={() => onViewPayload(msg.payload)} className="text-indigo-600 hover:text-indigo-900">Details</button></td>
              </tr>
            ))}
          </tbody>
        </table>
  );
};

const ServiceBusTable: React.FC<{ messages: ServiceBusMessage[], onViewPayload: (p: string) => void, isDlq?: boolean, emptyText: string }> = ({ messages, onViewPayload, isDlq, emptyText }) => {
  if (messages.length === 0) return <div className="p-8 text-center text-gray-500">{emptyText}</div>;

  return (
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Message ID</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Subject / Correlation</th>
              {isDlq && <th className="px-6 py-3 text-left text-xs font-medium text-red-500 uppercase tracking-wider">Failure Reason</th>}
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Enqueued</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Action</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {messages.map((msg) => (
              <tr key={msg.messageId} className="hover:bg-gray-50">
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 font-mono">{msg.messageId.substring(0, 8)}...</td>
                <td className="px-6 py-4 text-sm text-gray-900">
                  <div className="font-bold">{msg.subject || 'No Subject'}</div>
                  <div className="text-xs text-gray-500">Corr: {msg.correlationId}</div>
                  <div className="text-xs text-gray-400">Attempts: {msg.deliveryCount}</div>
                </td>
                {isDlq && (
                   <td className="px-6 py-4 text-sm text-red-600">
                     <div className="font-bold">{msg.deadLetterReason}</div>
                     <div className="text-xs max-w-xs truncate" title={msg.deadLetterErrorDescription}>{msg.deadLetterErrorDescription}</div>
                   </td>
                )}
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  <div>{new Date(msg.enqueuedTime).toLocaleString()}</div>
                </td>
                <td className="px-6 py-4 text-sm"><button onClick={() => onViewPayload(msg.body)} className="text-indigo-600 hover:text-indigo-900">Details</button></td>
              </tr>
            ))}
          </tbody>
        </table>
  );
};

export default SBInspector;

