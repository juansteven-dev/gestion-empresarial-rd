interface PlaceholderPageProps {
  section: string
  title: string
  description: string
}

export function PlaceholderPage({ section, title, description }: PlaceholderPageProps) {
  return (
    <section className="placeholder-page">
      <p className="eyebrow">{section}</p>
      <h1>{title}</h1>
      <p className="page-description">{description}</p>
      <div className="placeholder-card">
        <span className="placeholder-dot" aria-hidden="true" />
        <p>Vista preparada. La funcionalidad se añadirá por etapas.</p>
      </div>
    </section>
  )
}
