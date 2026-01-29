import { useState, useEffect, FormEvent } from 'react'
import { useCreateInstructor, useUpdateInstructor } from '../hooks/useInstructors'
import EventTypePicker from './EventTypePicker'
import type { Instructor } from '../api/instructors'

interface InstructorFormProps {
  selectedInstructor?: Instructor | null
  onSuccess?: () => void
}

const InstructorForm = ({ selectedInstructor, onSuccess }: InstructorFormProps) => {
  const [eventType, setEventType] = useState<string>('')
  const [formData, setFormData] = useState({
    instructorId: '',
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    employeeNumber: '',
    specialization: '',
    hireDate: '',
    notes: '',
  })
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({})

  const createInstructor = useCreateInstructor()
  const updateInstructor = useUpdateInstructor()

  // Pre-populate form when an instructor is selected
  useEffect(() => {
    if (selectedInstructor) {
      setEventType('InstructorUpdated')
      setFormData({
        instructorId: selectedInstructor.instructorId.toString(),
        firstName: selectedInstructor.firstName,
        lastName: selectedInstructor.lastName,
        email: selectedInstructor.email,
        phone: selectedInstructor.phone || '',
        employeeNumber: selectedInstructor.employeeNumber,
        specialization: selectedInstructor.specialization || '',
        hireDate: selectedInstructor.hireDate.split('T')[0],
        notes: selectedInstructor.notes || '',
      })
    }
  }, [selectedInstructor])

  const handleEventTypeChange = (type: string) => {
    setEventType(type)
    setValidationErrors({})

    // Reset form if switching from update to create or deactivate
    if (type === 'InstructorCreated' || type === 'InstructorDeactivated') {
      setFormData({
        instructorId: type === 'InstructorDeactivated' ? formData.instructorId : '',
        firstName: '',
        lastName: '',
        email: '',
        phone: '',
        employeeNumber: '',
        specialization: '',
        hireDate: '',
        notes: '',
      })
    }
  }

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {}

    if (eventType === 'InstructorCreated') {
      if (!formData.firstName.trim()) errors.firstName = 'First name is required'
      if (!formData.lastName.trim()) errors.lastName = 'Last name is required'
      if (!formData.email.trim()) {
        errors.email = 'Email is required'
      } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
        errors.email = 'Invalid email format'
      }
      if (!formData.employeeNumber.trim()) errors.employeeNumber = 'Employee number is required'
      if (!formData.hireDate) errors.hireDate = 'Hire date is required'
    } else if (eventType === 'InstructorUpdated') {
      if (!formData.instructorId.trim()) errors.instructorId = 'Instructor ID is required'
      // At least one field must be provided for update
      if (!formData.firstName && !formData.lastName && !formData.email && !formData.phone && !formData.specialization && !formData.notes) {
        errors.general = 'At least one field must be provided for update'
      }
      if (formData.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
        errors.email = 'Invalid email format'
      }
    } else if (eventType === 'InstructorDeactivated') {
      if (!formData.instructorId.trim()) errors.instructorId = 'Instructor ID is required'
      if (!formData.notes.trim()) errors.notes = 'Notes/reason for deactivation is required'
    }

    setValidationErrors(errors)
    return Object.keys(errors).length === 0
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    try {
      if (eventType === 'InstructorCreated') {
        await createInstructor.mutateAsync({
          firstName: formData.firstName,
          lastName: formData.lastName,
          email: formData.email,
          phone: formData.phone || undefined,
          employeeNumber: formData.employeeNumber,
          specialization: formData.specialization || undefined,
          hireDate: new Date(formData.hireDate).toISOString(),
          notes: formData.notes || undefined,
        })

        // Reset form after successful creation
        setFormData({
          instructorId: '',
          firstName: '',
          lastName: '',
          email: '',
          phone: '',
          employeeNumber: '',
          specialization: '',
          hireDate: '',
          notes: '',
        })
      } else if (eventType === 'InstructorUpdated') {
        await updateInstructor.mutateAsync({
          instructorId: formData.instructorId,
          firstName: formData.firstName || undefined,
          lastName: formData.lastName || undefined,
          email: formData.email || undefined,
          phone: formData.phone || undefined,
          specialization: formData.specialization || undefined,
          notes: formData.notes || undefined,
        })
      } else if (eventType === 'InstructorDeactivated') {
        // InstructorDeactivated updates the status with a reason
        await updateInstructor.mutateAsync({
          instructorId: formData.instructorId,
          notes: formData.notes,
        })
      }

      if (onSuccess) {
        onSuccess()
      }
    } catch (error) {
      console.error('Error submitting instructor form:', error)
      setValidationErrors({ general: 'An error occurred. Please try again.' })
    }
  }

  const handleInputChange = (field: string, value: string) => {
    setFormData((prev) => ({ ...prev, [field]: value }))
    // Clear validation error for this field
    setValidationErrors((prev) => {
      const newErrors = { ...prev }
      delete newErrors[field]
      delete newErrors.general
      return newErrors
    })
  }

  return (
    <div className="bg-white p-6 rounded-lg shadow-md">
      <h2 className="text-2xl font-bold mb-6 text-gray-800">Instructor Management</h2>

      <EventTypePicker
        entityType="Instructor"
        onEventTypeChange={handleEventTypeChange}
        initialEventType={selectedInstructor ? 'InstructorUpdated' : ''}
      />

      {eventType && (
        <form onSubmit={handleSubmit} className="space-y-4 mt-6">
          {(eventType === 'InstructorUpdated' || eventType === 'InstructorDeactivated') && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Instructor ID <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                value={formData.instructorId}
                onChange={(e) => handleInputChange('instructorId', e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Enter instructor ID"
                disabled={!!selectedInstructor}
              />
              {validationErrors.instructorId && (
                <p className="text-red-500 text-sm mt-1">{validationErrors.instructorId}</p>
              )}
            </div>
          )}

          {(eventType === 'InstructorCreated' || eventType === 'InstructorUpdated') && (
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                First Name {eventType === 'InstructorCreated' && <span className="text-red-500">*</span>}
              </label>
              <input
                type="text"
                value={formData.firstName}
                onChange={(e) => handleInputChange('firstName', e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Enter first name"
              />
              {validationErrors.firstName && (
                <p className="text-red-500 text-sm mt-1">{validationErrors.firstName}</p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Last Name {eventType === 'InstructorCreated' && <span className="text-red-500">*</span>}
              </label>
              <input
                type="text"
                value={formData.lastName}
                onChange={(e) => handleInputChange('lastName', e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Enter last name"
              />
              {validationErrors.lastName && (
                <p className="text-red-500 text-sm mt-1">{validationErrors.lastName}</p>
              )}
            </div>
          </div>
          )}

          {(eventType === 'InstructorCreated' || eventType === 'InstructorUpdated') && (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Email {eventType === 'InstructorCreated' && <span className="text-red-500">*</span>}
            </label>
            <input
              type="email"
              value={formData.email}
              onChange={(e) => handleInputChange('email', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="instructor@example.edu"
            />
            {validationErrors.email && (
              <p className="text-red-500 text-sm mt-1">{validationErrors.email}</p>
            )}
          </div>
          )}

          {(eventType === 'InstructorCreated' || eventType === 'InstructorUpdated') && (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Phone</label>
            <input
              type="tel"
              value={formData.phone}
              onChange={(e) => handleInputChange('phone', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="555-1234"
            />
          </div>
          )}

          {eventType === 'InstructorCreated' && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Employee Number <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                value={formData.employeeNumber}
                onChange={(e) => handleInputChange('employeeNumber', e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="EMP-12345"
              />
              {validationErrors.employeeNumber && (
                <p className="text-red-500 text-sm mt-1">{validationErrors.employeeNumber}</p>
              )}
            </div>
          )}

          {(eventType === 'InstructorCreated' || eventType === 'InstructorUpdated') && (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Specialization</label>
            <input
              type="text"
              value={formData.specialization}
              onChange={(e) => handleInputChange('specialization', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="e.g., Computer Science, Mathematics"
            />
          </div>
          )}

          {eventType === 'InstructorCreated' && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Hire Date <span className="text-red-500">*</span>
              </label>
              <input
                type="date"
                value={formData.hireDate}
                onChange={(e) => handleInputChange('hireDate', e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              {validationErrors.hireDate && (
                <p className="text-red-500 text-sm mt-1">{validationErrors.hireDate}</p>
              )}
            </div>
          )}

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Notes{eventType === 'InstructorDeactivated' && <span className="text-red-500">*</span>}
            </label>
            <textarea
              value={formData.notes}
              onChange={(e) => handleInputChange('notes', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              rows={3}
              placeholder="Additional notes about the instructor..."
            />
            {validationErrors.notes && (
              <p className="text-red-500 text-sm mt-1">{validationErrors.notes}</p>
            )}
          </div>

          {validationErrors.general && (
            <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
              {validationErrors.general}
            </div>
          )}

          <button
            type="submit"
            disabled={createInstructor.isPending || updateInstructor.isPending}
            className="w-full bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-400"
          >
            {createInstructor.isPending || updateInstructor.isPending
              ? 'Submitting...'
              : eventType === 'InstructorCreated'
              ? 'Create Instructor'
              : eventType === 'InstructorDeactivated'
              ? 'Deactivate Instructor'
              : 'Update Instructor'}
          </button>

          {(createInstructor.isError || updateInstructor.isError) && (
            <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
              Error: {createInstructor.error?.message || updateInstructor.error?.message}
            </div>
          )}

          {(createInstructor.isSuccess || updateInstructor.isSuccess) && (
            <div className="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded">
              {eventType === 'InstructorCreated' ? 'Instructor created successfully!' : eventType === 'InstructorDeactivated' ? 'Instructor deactivated successfully!' : 'Instructor updated successfully!'}
            </div>
          )}
        </form>
      )}
    </div>
  )
}

export default InstructorForm
