/** Downloads GET /api/export and saves it as a file - no JSON envelope, so this bypasses apiClient. */
export async function downloadExport(): Promise<void> {
  const response = await fetch('/api/export', { credentials: 'include' })

  if (!response.ok) {
    throw new Error(`Export failed with status ${response.status}`)
  }

  const blob = await response.blob()
  const url = URL.createObjectURL(blob)
  try {
    const link = document.createElement('a')
    link.href = url
    link.download = 'paraglog-export.zip'
    document.body.appendChild(link)
    link.click()
    link.remove()
  } finally {
    URL.revokeObjectURL(url)
  }
}
