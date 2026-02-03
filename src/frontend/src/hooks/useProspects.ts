import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { prospectsApi, type Prospect, type CreateProspectRequest, type UpdateProspectRequest } from '../api/prospects'

const PROSPECTS_QUERY_KEY = ['prospects']

export const useProspects = () => {
  return useQuery<Prospect[], Error>({
    queryKey: PROSPECTS_QUERY_KEY,
    queryFn: () => prospectsApi.getProspects(),
  })
}

export const useProspect = (prospectId: string | null) => {
  return useQuery<Prospect, Error>({
    queryKey: ['prospect', prospectId],
    queryFn: () => prospectsApi.getProspect(prospectId!),
    enabled: !!prospectId,
  })
}

export const useCreateProspect = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateProspectRequest) => prospectsApi.createProspect(request),
    onSuccess: () => {
      // Don't invalidate immediately - wait for WebSocket event
      // queryClient.invalidateQueries({ queryKey: PROSPECTS_QUERY_KEY })
      console.log('Command sent successfully. Waiting for WebSocket update...')
      
      // Fallback: Invalidate after delay in case WebSocket misses
      setTimeout(() => {
        queryClient.invalidateQueries({ queryKey: PROSPECTS_QUERY_KEY })
      }, 1000)
    },
  })
}

export const useUpdateProspect = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: UpdateProspectRequest) => prospectsApi.updateProspect(request),
    onSuccess: () => {
      // Don't invalidate immediately - wait for WebSocket event
      // queryClient.invalidateQueries({ queryKey: PROSPECTS_QUERY_KEY })
      // queryClient.invalidateQueries({ queryKey: ['prospect'] })
      console.log('Command sent successfully. Waiting for WebSocket update...')

      // Fallback: Invalidate after delay in case WebSocket misses
      setTimeout(() => {
        queryClient.invalidateQueries({ queryKey: PROSPECTS_QUERY_KEY })
        queryClient.invalidateQueries({ queryKey: ['prospect'] })
      }, 1000)
    },
  })
}

// Hook to invalidate prospect queries when WebSocket events are received
export const useInvalidateProspects = () => {
  const queryClient = useQueryClient()

  return () => {
    queryClient.invalidateQueries({ queryKey: PROSPECTS_QUERY_KEY })
    queryClient.invalidateQueries({ queryKey: ['prospect'] })
  }
}
