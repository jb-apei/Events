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
    onMutate: async (newProspect) => {
      // 1. Cancel outgoing refetches so they don't overwrite our optimistic update
      await queryClient.cancelQueries({ queryKey: PROSPECTS_QUERY_KEY })

      // 2. Snapshot the previous value
      const previousProspects = queryClient.getQueryData<Prospect[]>(PROSPECTS_QUERY_KEY)

      // 3. Optimistically update to the new value
      if (previousProspects) {
        queryClient.setQueryData<Prospect[]>(PROSPECTS_QUERY_KEY, (old) => {
          if (!old) return []
          // Create a temporary "optimistic" prospect
          const optimisticProspect: Prospect = {
            prospectId: -1, // Temp ID (negative to indicate local/optimistic)
            firstName: newProspect.firstName,
            lastName: newProspect.lastName,
            email: newProspect.email,
            phone: newProspect.phone || '',
            contacts: 0,
            status: 'New',
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString()
          }
          return [optimisticProspect, ...old]
        })
      }

      return { previousProspects }
    },
    onError: (_err, _newTodo, context) => {
      // If the mutation fails, use the context returned from onMutate to roll back
      if (context?.previousProspects) {
        queryClient.setQueryData(PROSPECTS_QUERY_KEY, context.previousProspects)
      }
    },
    onSuccess: () => {
      console.log('Command sent successfully. UI optimism applied.')
    },
  })
}

export const useUpdateProspect = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: UpdateProspectRequest) => prospectsApi.updateProspect(request),
    onMutate: async (updatedProspect) => {
      await queryClient.cancelQueries({ queryKey: PROSPECTS_QUERY_KEY })
      const previousProspects = queryClient.getQueryData<Prospect[]>(PROSPECTS_QUERY_KEY)

      if (previousProspects) {
        queryClient.setQueryData<Prospect[]>(PROSPECTS_QUERY_KEY, (old) => {
          if (!old) return []
          return old.map((p) => 
            p.prospectId.toString() === updatedProspect.prospectId 
              ? { ...p, ...updatedProspect, updatedAt: new Date().toISOString() } 
              : p
          )
        })
      }

      return { previousProspects }
    },
    onError: (_err, _newTodo, context) => {
      if (context?.previousProspects) {
        queryClient.setQueryData(PROSPECTS_QUERY_KEY, context.previousProspects)
      }
    },
    onSuccess: () => {
      console.log('Command sent successfully. UI optimism applied.')
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
