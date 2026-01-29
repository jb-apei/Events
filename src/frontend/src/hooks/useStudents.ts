import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { studentsApi, CreateStudentRequest, UpdateStudentRequest } from '../api/students'

export const useStudents = () => {
  return useQuery({
    queryKey: ['students'],
    queryFn: () => studentsApi.getStudents(),
  })
}

export const useStudent = (studentId: string) => {
  return useQuery({
    queryKey: ['students', studentId],
    queryFn: () => studentsApi.getStudent(studentId),
    enabled: !!studentId,
  })
}

export const useCreateStudent = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateStudentRequest) => studentsApi.createStudent(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['students'] })
    },
  })
}

export const useUpdateStudent = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: UpdateStudentRequest) => studentsApi.updateStudent(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['students'] })
    },
  })
}
