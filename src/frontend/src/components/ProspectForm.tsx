import { useState, useEffect, FormEvent } from 'react'
import { useCreateProspect, useUpdateProspect } from '../hooks/useProspects'
import EventTypePicker from './EventTypePicker'
import type { Prospect } from '../api/prospects'

interface ProspectFormProps {
  selectedProspect?: Prospect | null
  onSuccess?: () => void
}

const ProspectForm = ({ selectedProspect, onSuccess }: ProspectFormProps) => {
  const [eventType, setEventType] = useState<string>('')
  const [formData, setFormData] = useState({
    prospectId: '',
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    notes: '',
  })
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({})

  const createProspect = useCreateProspect()
  const updateProspect = useUpdateProspect()

  // Pre-populate form when a prospect is selected
  useEffect(() => {
    if (selectedProspect) {
      setEventType('ProspectUpdated')
      setFormData({
        prospectId: selectedProspect.prospectId.toString(),
        firstName: selectedProspect.firstName,
        lastName: selectedProspect.lastName,
        email: selectedProspect.email,
        phone: selectedProspect.phone || '',
        notes: selectedProspect.notes || '',
      })
    }
  }, [selectedProspect])

  const handleEventTypeChange = (type: string) => {
    setEventType(type)
    setValidationErrors({})

    // Reset form if switching from update to create
    if (type === 'ProspectCreated') {
      setFormData({
        prospectId: '',
        firstName: '',
        lastName: '',
        email: '',
        phone: '',
        notes: '',
      })
    }
  }

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {}

    if (eventType === 'ProspectCreated') {
      if (!formData.firstName.trim()) errors.firstName = 'First name is required'
      if (!formData.lastName.trim()) errors.lastName = 'Last name is required'
      if (!formData.email.trim()) {
        errors.email = 'Email is required'
      } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
        errors.email = 'Invalid email format'
      }
    } else if (eventType === 'ProspectUpdated') {
      if (!formData.prospectId.trim()) errors.prospectId = 'Prospect ID is required'
      // At least one field must be provided for update
      if (!formData.firstName && !formData.lastName && !formData.email && !formData.phone && !formData.notes) {
        errors.general = 'At least one field must be provided for update'
      }
      if (formData.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
        errors.email = 'Invalid email format'
      }
    }

    setValidationErrors(errors)
    return Object.keys(errors).length === 0
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    try {
      if (eventType === 'ProspectCreated') {
        await createProspect.mutateAsync({
          firstName: formData.firstName,
          lastName: formData.lastName,
          email: formData.email,
          phone: formData.phone || undefined,
          notes: formData.notes || undefined,
        })

        // Reset form after successful creation
        setFormData({
          prospectId: '',
          firstName: '',
          lastName: '',
          email: '',
          phone: '',
          notes: '',
        })
        setEventType('')
      } else if (eventType === 'ProspectUpdated') {
        await updateProspect.mutateAsync({
          prospectId: formData.prospectId,
          firstName: formData.firstName || undefined,
          lastName: formData.lastName || undefined,
          email: formData.email || undefined,
          phone: formData.phone || undefined,
          notes: formData.notes || undefined,
        })
      }

      onSuccess?.()
    } catch (error: any) {
      setValidationErrors({
        general: error.response?.data?.message || 'An error occurred. Please try again.',
      })
    }
  }

  const isLoading = createProspect.isPending || updateProspect.isPending

  return (
    <div className="card">
      <h2>Prospect Form</h2>
      <form onSubmit={handleSubmit}>
        <EventTypePicker
          entityType="Prospect"
          onEventTypeChange={handleEventTypeChange}
          initialEventType={eventType}
        />

        {eventType === 'ProspectUpdated' && (
          <div className="form-group">
            <label htmlFor="prospectId">Prospect ID *</label>
            <input
              id="prospectId"
              type="text"
              value={formData.prospectId}
              onChange={(e) => setFormData({ ...formData, prospectId: e.target.value })}
              disabled={!!selectedProspect}
            />
            {validationErrors.prospectId && (
              <div className="error-message">{validationErrors.prospectId}</div>
            )}
          </div>
        )}

        {eventType && (
          <>
            <div className="form-group">
              <label htmlFor="firstName">
                First Name {eventType === 'ProspectCreated' ? '*' : ''}
              </label>
              <input
                id="firstName"
                type="text"
                value={formData.firstName}
                onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
              />
              {validationErrors.firstName && (
                <div className="error-message">{validationErrors.firstName}</div>
              )}
            </div>

            <div className="form-group">
              <label htmlFor="lastName">
                Last Name {eventType === 'ProspectCreated' ? '*' : ''}
              </label>
              <input
                id="lastName"
                type="text"
                value={formData.lastName}
                onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
              />
              {validationErrors.lastName && (
                <div className="error-message">{validationErrors.lastName}</div>
              )}
            </div>

            <div className="form-group">
              <label htmlFor="email">
                Email {eventType === 'ProspectCreated' ? '*' : ''}
              </label>
              <input
                id="email"
                type="email"
                value={formData.email}
                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
              />
              {validationErrors.email && (
                <div className="error-message">{validationErrors.email}</div>
              )}
            </div>

            <div className="form-group">
              <label htmlFor="phone">Phone</label>
              <input
                id="phone"
                type="tel"
                value={formData.phone}
                onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
              />
            </div>

            <div className="form-group">
              <label htmlFor="notes">Notes</label>
              <textarea
                id="notes"
                value={formData.notes}
                onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                rows={3}
                style={{ width: '100%', resize: 'vertical' }}
              />
            </div>

            {validationErrors.general && (
              <div className="error-message">{validationErrors.general}</div>
            )}

            <button type="submit" className="btn btn-primary" disabled={isLoading}>
              {isLoading
                ? 'Submitting...'
                : eventType === 'ProspectCreated'
                ? 'Create Prospect'
                : 'Update Prospect'}
            </button>
          </>
        )}
      </form>
    </div>
  )
}

export default ProspectForm
