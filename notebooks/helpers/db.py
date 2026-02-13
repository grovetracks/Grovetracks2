"""PostgreSQL database access for seed compositions."""

import os
import json
import uuid
from datetime import datetime, timezone
from contextlib import contextmanager
from .models import Composition

import psycopg2
import psycopg2.extras


def _conn_params() -> dict:
    return {
        "host": os.environ.get("DB_HOST", "localhost"),
        "port": int(os.environ.get("DB_PORT", "5432")),
        "dbname": os.environ.get("DB_NAME", "grovetracks"),
        "user": os.environ.get("DB_USER", "grovetracks"),
        "password": os.environ.get("DB_PASSWORD", "grovetracks_dev"),
    }


@contextmanager
def get_connection():
    """Context manager for database connections."""
    conn = psycopg2.connect(**_conn_params())
    try:
        yield conn
    finally:
        conn.close()


def get_curated(word: str, limit: int = 50) -> list[Composition]:
    """Load curated compositions for a word, ordered by quality score descending."""
    with get_connection() as conn:
        with conn.cursor(cursor_factory=psycopg2.extras.DictCursor) as cur:
            cur.execute(
                """
                SELECT composition_json, quality_score, stroke_count, total_point_count
                FROM seed_compositions
                WHERE word = %s AND source_type = 'curated'
                ORDER BY quality_score DESC
                LIMIT %s
                """,
                (word, limit),
            )
            rows = cur.fetchall()

    results = []
    for row in rows:
        data = row["composition_json"]
        if isinstance(data, str):
            data = json.loads(data)
        results.append(Composition.from_dict(data))
    return results


def get_curated_words() -> list[str]:
    """Get all words that have curated compositions."""
    with get_connection() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """
                SELECT DISTINCT word FROM seed_compositions
                WHERE source_type = 'curated'
                ORDER BY word
                """
            )
            return [row[0] for row in cur.fetchall()]


def get_curated_stats(word: str) -> dict:
    """Get statistics for curated compositions of a word."""
    with get_connection() as conn:
        with conn.cursor(cursor_factory=psycopg2.extras.DictCursor) as cur:
            cur.execute(
                """
                SELECT COUNT(*) as total,
                       AVG(quality_score) as avg_quality,
                       MIN(quality_score) as min_quality,
                       MAX(quality_score) as max_quality,
                       AVG(stroke_count) as avg_strokes,
                       AVG(total_point_count) as avg_points
                FROM seed_compositions
                WHERE word = %s AND source_type = 'curated'
                """,
                (word,),
            )
            row = cur.fetchone()
            return dict(row) if row else {}


def save_compositions(
    word: str,
    compositions: list[Composition],
    generation_method: str = "notebook-ollama",
    quality_scores: list[float] | None = None,
) -> int:
    """Save validated compositions to seed_compositions table. Returns count saved."""
    from .validate import validate, count_strokes, count_points

    saved = 0
    with get_connection() as conn:
        with conn.cursor() as cur:
            for i, comp in enumerate(compositions):
                is_valid, score = validate(comp)
                if not is_valid:
                    continue

                if quality_scores and i < len(quality_scores):
                    score = quality_scores[i]

                comp_json = json.dumps(comp.to_dict())
                cur.execute(
                    """
                    INSERT INTO seed_compositions
                    (id, word, source_key_id, quality_score, stroke_count, total_point_count,
                     composition_json, curated_at, source_type, generation_method)
                    VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
                    """,
                    (
                        str(uuid.uuid4()),
                        word,
                        "ai-generated",
                        score,
                        count_strokes(comp),
                        count_points(comp),
                        comp_json,
                        datetime.now(timezone.utc),
                        "ai-generated",
                        generation_method,
                    ),
                )
                saved += 1
        conn.commit()
    return saved
