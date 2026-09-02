import { useRef } from 'react'
import { Button } from '../../../components/ui/Button'
import { EmptyState } from '../../../components/ui/EmptyState'
import type { PanelNav } from '../../../lib/panel/types'
import { makeActivityDetailView } from './ActivityDetail'
import { makeActivityCreateView } from './ActivityForm'
import { ActivityListItem } from './ActivityListItem'
import { useActivities } from '../queries'
import '../activities.css'

const SCROLL_THRESHOLD_PX = 120

interface ActivityListProps {
  nav: PanelNav
}

export function ActivityList({ nav }: ActivityListProps) {
  const { data, isLoading, isError, fetchNextPage, hasNextPage, isFetchingNextPage } = useActivities()
  const scrollRef = useRef<HTMLUListElement>(null)

  const items = data?.pages.flatMap((page) => page.items) ?? []

  function handleScroll() {
    const el = scrollRef.current
    if (!el || !hasNextPage || isFetchingNextPage) return
    if (el.scrollHeight - el.scrollTop - el.clientHeight < SCROLL_THRESHOLD_PX) {
      void fetchNextPage()
    }
  }

  return (
    <div className="activity-list">
      {isLoading ? (
        <p>Loading…</p>
      ) : isError ? (
        <EmptyState title="Couldn't load activities" description="Please try again." />
      ) : items.length === 0 ? (
        <EmptyState
          title="No activities yet"
          description="Log a flight or ground handling session to get started."
        />
      ) : (
        <ul className="activity-list-scroll" ref={scrollRef} onScroll={handleScroll}>
          {items.map((activity) => (
            <ActivityListItem
              key={activity.id}
              activity={activity}
              onSelect={() => nav.push(makeActivityDetailView(activity.id))}
            />
          ))}
          {isFetchingNextPage && <li className="activity-list-loading-more">Loading more…</li>}
        </ul>
      )}

      <div className="activity-list-footer">
        <Button variant="primary" onClick={() => nav.push(makeActivityCreateView())}>
          Create/import
        </Button>
      </div>
    </div>
  )
}
