import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { instructorsApi, CreateInstructorRequest, UpdateInstructorRequest } from '../api/instructors'

export const useInstructors = () => {
  return useQuery({
    queryKey: ['instructors'],
    queryFn: () => instructorsApi.getInstructors(),
  })
}

export const useInstructor = (instructorId: string) => {
  return useQuery({
    queryKey: ['instructors', instructorId],
    queryFn: () => instructorsApi.getInstructor(instructorId),
    enabled: !!instructorId,
  })
}

export const useCreateInstructor = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateInstructorRequest) => instructorsApi.createInstructor(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['instructors'] })
    },
  })
}

export const useUpdateInstructor = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: UpdateInstructorRequest) => instructorsApi.updateInstructor(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['instructors'] })
    },
  })
}
