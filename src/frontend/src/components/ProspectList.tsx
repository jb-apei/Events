import { useProspects } from '../hooks/useProspects'
import type { Prospect } from '../api/prospects'

interface ProspectListProps {
  onSelectProspect: (prospect: Prospect) => void
  selectedProspect?: Prospect | null
}

const ProspectList = ({ onSelectProspect, selectedProspect }: ProspectListProps) => {
  const { data: prospects, isLoading, error } = useProspects()

  if (isLoading) {
    return (
      <div className="card">
        <h2>Prospects</h2>
        <div className="loading">Loading prospects...</div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="card">
        <h2>Prospects</h2>
        <div className="error-message">
          Failed to load prospects: {error.message}
        </div>
      </div>
    )
  }

  if (!prospects || !Array.isArray(prospects) || prospects.length === 0) {
    return (
      <div className="card">
        <h2>Prospects</h2>
        <div className="empty-state">
          <p>No prospects found</p>
          <p>Create your first prospect using the form</p>
        </div>
      </div>
    )
  }

  return (
    <div className="card">
      <h2>Prospects ({prospects.length})</h2>
      <div className="prospect-list">
        {prospects.map((prospect) => (
          <div
            key={prospect.prospectId}
            className={`prospect-item ${
              selectedProspect?.prospectId === prospect.prospectId ? 'selected' : ''
            }`}
            onClick={() => onSelectProspect(prospect)}
          >
            <h3>
              {prospect.firstName} {prospect.lastName}
            </h3>
            <p>📧 {prospect.email}</p>
            {prospect.phone && <p>📱 {prospect.phone}</p>}
            <p>
              <strong>Status:</strong> {prospect.status}
            </p>
            {prospect.notes && (
              <p style={{ fontStyle: 'italic', fontSize: '0.9rem', margin: '4px 0' }}>
                📝 {prospect.notes}
              </p>
            )}
            <p style={{ fontSize: '0.75rem', color: '#999' }}>
              ID: {prospect.prospectId}
            </p>
          </div>
        ))}
      </div>
    </div>
  )
}

export default ProspectList
