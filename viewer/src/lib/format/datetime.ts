const dateTimeFormatter = new Intl.DateTimeFormat(undefined, {
  dateStyle: 'medium',
  timeStyle: 'short',
})

export function formatDateTime(iso: string): string {
  return dateTimeFormatter.format(new Date(iso))
}

/** Value for an <input type="datetime-local">, in the browser's local time. */
export function toDatetimeLocalValue(iso: string): string {
  const date = new Date(iso)
  const offsetMs = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16)
}

/** Parses an <input type="datetime-local"> value (local time, no zone) into an ISO UTC string. */
export function fromDatetimeLocalValue(value: string): string {
  return new Date(value).toISOString()
}

export function localDateOf(datetimeLocalValue: string): string {
  return datetimeLocalValue.slice(0, 10)
}

export function browserTimeZone(): string {
  return Intl.DateTimeFormat().resolvedOptions().timeZone
}
