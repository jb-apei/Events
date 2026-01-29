import { useState, useEffect, FormEvent } from 'react'
import { useCreateStudent, useUpdateStudent } from '../hooks/useStudents'
import EventTypePicker from './EventTypePicker'
import type { Student } from '../api/students'

interface StudentFormProps {
  selectedStudent?: Student | null
  onSuccess?: () => void
}

const StudentForm = ({ selectedStudent, onSuccess }: StudentFormProps) => {
  const [eventType, setEventType] = useState<string>('')
  const [formData, setFormData] = useState({
    studentId: '',
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    studentNumber: '',
    enrollmentDate: '',
    expectedGraduationDate: '',
    notes: '',
  })
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({})

  const createStudent = useCreateStudent()
  const updateStudent = useUpdateStudent()

  // Pre-populate form when a student is selected
  useEffect(() => {
    if (selectedStudent) {
      setEventType('StudentUpdated')
      setFormData({
        studentId: selectedStudent.studentId.toString(),
        firstName: selectedStudent.firstName,
        lastName: selectedStudent.lastName,
        email: selectedStudent.email,
        phone: selectedStudent.phone || '',
        studentNumber: selectedStudent.studentNumber,
        enrollmentDate: selectedStudent.enrollmentDate.split('T')[0],
        expectedGraduationDate: selectedStudent.expectedGraduationDate?.split('T')[0] || '',
        notes: selectedStudent.notes || '',
      })
    }
  }, [selectedStudent])

  const handleEventTypeChange = (type: string) => {
    setEventType(type)
    setValidationErrors({})

    // Reset form if switching from update to create or change status
    if (type === 'StudentCreated' || type === 'StudentChanged') {
      setFormData({
        studentId: type === 'StudentChanged' ? formData.studentId : '',
        firstName: '',
        lastName: '',
        email: '',
        phone: '',
        studentNumber: '',
        enrollmentDate: '',
        expectedGraduationDate: '',
        notes: '',
      })
    }
  }

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {}

    if (eventType === 'StudentCreated') {
      if (!formData.firstName.trim()) errors.firstName = 'First name is required'
      if (!formData.lastName.trim()) errors.lastName = 'Last name is required'
      if (!formData.email.trim()) {
        errors.email = 'Email is required'
      } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
        errors.email = 'Invalid email format'
      }
      if (!formData.studentNumber.trim()) errors.studentNumber = 'Student number is required'
      if (!formData.enrollmentDate) errors.enrollmentDate = 'Enrollment date is required'
    } else if (eventType === 'StudentUpdated') {
      if (!formData.studentId.trim()) errors.studentId = 'Student ID is required'
      // At least one field must be provided for update
      if (!formData.firstName && !formData.lastName && !formData.email && !formData.phone && !formData.expectedGraduationDate && !formData.notes) {
        errors.general = 'At least one field must be provided for update'
      }
      if (formData.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
        errors.email = 'Invalid email format'
      }
    } else if (eventType === 'StudentChanged') {
      if (!formData.studentId.trim()) errors.studentId = 'Student ID is required'
      if (!formData.notes.trim()) errors.notes = 'Notes/reason for status change is required'
    }

    setValidationErrors(errors)
    return Object.keys(errors).length === 0
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    try {
      if (eventType === 'StudentCreated') {
        await createStudent.mutateAsync({
          firstName: formData.firstName,
          lastName: formData.lastName,
          email: formData.email,
          phone: formData.phone || undefined,
          studentNumber: formData.studentNumber,
          enrollmentDate: new Date(formData.enrollmentDate).toISOString(),
          expectedGraduationDate: formData.expectedGraduationDate ? new Date(formData.expectedGraduationDate).toISOString() : undefined,
          notes: formData.notes || undefined,
        })

        // Reset form after successful creation
        setFormData({
          studentId: '',
          firstName: '',
          lastName: '',
          email: '',
          phone: '',
          studentNumber: '',
          enrollmentDate: '',
          expectedGraduationDate: '',
          notes: '',
        })
      } else if (eventType === 'StudentUpdated') {
        await updateStudent.mutateAsync({
          studentId: formData.studentId,
          firstName: formData.firstName || undefined,
          lastName: formData.lastName || undefined,
          email: formData.email || undefined,
          phone: formData.phone || undefined,
          expectedGraduationDate: formData.expectedGraduationDate ? new Date(formData.expectedGraduationDate).toISOString() : undefined,
          notes: formData.notes || undefined,
        })
      } else if (eventType === 'StudentChanged') {
        // StudentChanged typically updates status with a reason
        await updateStudent.mutateAsync({
          studentId: formData.studentId,
          notes: formData.notes,
        })
      }

      if (onSuccess) {
        onSuccess()
      }
    } catch (error) {
      console.error('Error submitting student form:', error)
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
      <h2 className="text-2xl font-bold mb-6 text-gray-800">Student Management</h2>

      <EventTypePicker
        entityType="Student"
        onEventTypeChange={handleEventTypeChange}
        initialEventType={selectedStudent ? 'StudentUpdated' : ''}
      />

      {eventType && (
        <form onSubmit={handleSubmit} className="space-y-4 mt-6">
          {(eventType === 'StudentUpdated' || eventType === 'StudentChanged') && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Student ID <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                value={formData.studentId}
                onChange={(e) => handleInputChange('studentId', e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Enter student ID"
                disabled={!!selectedStudent}
              />
              {validationErrors.studentId && (
                <p className="text-red-500 text-sm mt-1">{validationErrors.studentId}</p>
              )}
            </div>
          )}

          {(eventType === 'StudentCreated' || eventType === 'StudentUpdated') && (
            <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                First Name {eventType === 'StudentCreated' && <span className="text-red-500">*</span>}
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
                Last Name {eventType === 'StudentCreated' && <span className="text-red-500">*</span>}
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

          {(eventType === 'StudentCreated' || eventType === 'StudentUpdated') && (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Email {eventType === 'StudentCreated' && <span className="text-red-500">*</span>}
            </label>
            <input
              type="email"
              value={formData.email}
              onChange={(e) => handleInputChange('email', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="student@example.edu"
            />
            {validationErrors.email && (
              <p className="text-red-500 text-sm mt-1">{validationErrors.email}</p>
            )}
          </div>
          )}

          {(eventType === 'StudentCreated' || eventType === 'StudentUpdated') && (
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

          {eventType === 'StudentCreated' && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Student Number <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                value={formData.studentNumber}
                onChange={(e) => handleInputChange('studentNumber', e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="STU-12345"
              />
              {validationErrors.studentNumber && (
                <p className="text-red-500 text-sm mt-1">{validationErrors.studentNumber}</p>
              )}
            </div>
          )}

          {eventType === 'StudentCreated' && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Enrollment Date <span className="text-red-500">*</span>
              </label>
              <input
                type="date"
                value={formData.enrollmentDate}
                onChange={(e) => handleInputChange('enrollmentDate', e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              {validationErrors.enrollmentDate && (
                <p className="text-red-500 text-sm mt-1">{validationErrors.enrollmentDate}</p>
              )}
            </div>
          )}

          {(eventType === 'StudentCreated' || eventType === 'StudentUpdated') && (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Expected Graduation Date
            </label>
            <input
              type="date"
              value={formData.expectedGraduationDate}
              onChange={(e) => handleInputChange('expectedGraduationDate', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          )}

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Notes{eventType === 'StudentChanged' && <span className="text-red-500">*</span>}
            </label>
            <textarea
              value={formData.notes}
              onChange={(e) => handleInputChange('notes', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              rows={3}
              placeholder="Additional notes about the student..."
            />
          </div>

          {validationErrors.general && (
            <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
              {validationErrors.general}
            </div>
          )}

          <button
            type="submit"
            disabled={createStudent.isPending || updateStudent.isPending}
            className="w-full bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-400"
          >
            {createStudent.isPending || updateStudent.isPending
              ? 'Submitting...'
              : eventType === 'StudentCreated'
              ? 'Create Student'
              : eventType === 'StudentChanged'
              ? 'Change Student Status'
              : 'Update Student'}
          </button>

          {(createStudent.isError || updateStudent.isError) && (
            <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
              Error: {createStudent.error?.message || updateStudent.error?.message}
            </div>
          )}

          {(createStudent.isSuccess || updateStudent.isSuccess) && (
            <div className="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded">
              {eventType === 'StudentCreated' ? 'Student created successfully!' : eventType === 'StudentChanged' ? 'Student status changed successfully!' : 'Student updated successfully!'}
            </div>
          )}
        </form>
      )}
    </div>
  )
}

export default StudentForm
