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
      // Invalidate prospects list to trigger refetch
      queryClient.invalidateQueries({ queryKey: PROSPECTS_QUERY_KEY })
    },
  })
}

export const useUpdateProspect = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: UpdateProspectRequest) => prospectsApi.updateProspect(request),
    onSuccess: () => {
      // Invalidate prospects list and individual prospect queries
      queryClient.invalidateQueries({ queryKey: PROSPECTS_QUERY_KEY })
      queryClient.invalidateQueries({ queryKey: ['prospect'] })
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
