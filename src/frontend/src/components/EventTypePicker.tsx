interface EventTypePickerProps {
  entityType: 'Prospect' | 'Student' | 'Instructor'
  onEventTypeChange: (eventType: string) => void
  initialEventType?: string
}

const EVENT_TYPES_MAP = {
  Prospect: [
    { value: 'ProspectCreated', label: 'Create New Prospect' },
    { value: 'ProspectUpdated', label: 'Update Existing Prospect' },
    { value: 'ProspectMerged', label: 'Merge Prospect' },
  ],
  Student: [
    { value: 'StudentCreated', label: 'Create New Student' },
    { value: 'StudentUpdated', label: 'Update Existing Student' },
    { value: 'StudentChanged', label: 'Change Student Status' },
  ],
  Instructor: [
    { value: 'InstructorCreated', label: 'Create New Instructor' },
    { value: 'InstructorUpdated', label: 'Update Existing Instructor' },
    { value: 'InstructorDeactivated', label: 'Deactivate Instructor' },
  ],
}

const EventTypePicker = ({ entityType, onEventTypeChange, initialEventType }: EventTypePickerProps) => {
  const eventTypes = EVENT_TYPES_MAP[entityType]

  return (
    <div className="form-group">
      <label htmlFor="eventType">Event Type</label>
      <select
        id="eventType"
        value={initialEventType || ''}
        onChange={(e) => onEventTypeChange(e.target.value)}
      >
        <option value="">Select an event type...</option>
        {eventTypes.map((type) => (
          <option key={type.value} value={type.value}>
            {type.label}
          </option>
        ))}
      </select>
    </div>
  )
}

export default EventTypePicker
