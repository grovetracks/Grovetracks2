"""Image loading, downloading, and display utilities."""

from __future__ import annotations

import io
from pathlib import Path

import httpx
import matplotlib.pyplot as plt
import numpy as np
from PIL import Image

_HTTP_HEADERS = {"User-Agent": "Grovetracks/2.0 (image-tracing-notebook)"}


def load_image(path_or_url: str) -> Image.Image:
    """Load an image from a local file path or URL."""
    if path_or_url.startswith(("http://", "https://")):
        response = httpx.get(path_or_url, headers=_HTTP_HEADERS, follow_redirects=True, timeout=30.0)
        response.raise_for_status()
        return Image.open(io.BytesIO(response.content)).convert("RGB")
    return Image.open(path_or_url).convert("RGB")


def download_image(url: str, save_dir: str = "images/", filename: str | None = None) -> Path:
    """Download an image from URL and save locally. Returns the saved file path."""
    save_path = Path(save_dir)
    save_path.mkdir(parents=True, exist_ok=True)

    response = httpx.get(url, headers=_HTTP_HEADERS, follow_redirects=True, timeout=30.0)
    response.raise_for_status()

    if filename is None:
        # Extract filename from URL or generate one
        url_path = url.split("?")[0].split("/")[-1]
        if "." in url_path and len(url_path) < 100:
            filename = url_path
        else:
            content_type = response.headers.get("content-type", "image/jpeg")
            ext = content_type.split("/")[-1].split(";")[0]
            if ext not in ("jpeg", "jpg", "png", "webp", "gif"):
                ext = "jpg"
            import hashlib
            filename = hashlib.md5(url.encode()).hexdigest()[:12] + f".{ext}"

    file_path = save_path / filename
    file_path.write_bytes(response.content)
    return file_path


def search_images(query: str, count: int = 10) -> list[dict]:
    """Search for images using Unsplash API (free, no auth needed for demo endpoint).

    Returns list of dicts: [{url, thumb_url, description, photographer, unsplash_link}]
    """
    params = {"query": query, "per_page": min(count, 30)}

    try:
        response = httpx.get(
            "https://unsplash.com/napi/search/photos",
            params=params,
            headers={"Accept": "application/json"},
            timeout=15.0,
        )
        response.raise_for_status()
        data = response.json()

        results = []
        for photo in data.get("results", []):
            urls = photo.get("urls", {})
            user = photo.get("user", {})
            results.append({
                "url": urls.get("regular", urls.get("full", "")),
                "thumb_url": urls.get("thumb", urls.get("small", "")),
                "description": photo.get("alt_description", photo.get("description", "")) or "",
                "photographer": user.get("name", "Unknown"),
                "unsplash_link": photo.get("links", {}).get("html", ""),
            })
        return results
    except Exception as e:
        print(f"Unsplash search failed: {e}")
        print("Falling back to direct URL loading — use load_image(url) instead.")
        return []


def show_image(img: Image.Image, title: str | None = None, figsize: tuple = (6, 6)) -> None:
    """Display a PIL Image inline in the notebook."""
    fig, ax = plt.subplots(1, 1, figsize=figsize)
    ax.imshow(np.array(img))
    ax.set_xticks([])
    ax.set_yticks([])
    if title:
        ax.set_title(title, fontsize=12)
    plt.tight_layout()
    plt.show()


def show_image_grid(
    images: list[Image.Image],
    cols: int = 5,
    title: str | None = None,
    titles: list[str] | None = None,
    figsize_per_cell: float = 3.0,
) -> None:
    """Display multiple PIL Images in a grid."""
    import math

    n = len(images)
    if n == 0:
        print("No images to display.")
        return

    rows = math.ceil(n / cols)
    fig, axes = plt.subplots(rows, cols, figsize=(figsize_per_cell * cols, figsize_per_cell * rows))

    if rows == 1 and cols == 1:
        axes = np.array([[axes]])
    elif rows == 1:
        axes = axes[np.newaxis, :]
    elif cols == 1:
        axes = axes[:, np.newaxis]

    for i in range(rows * cols):
        r, c = divmod(i, cols)
        ax = axes[r, c]
        if i < n:
            ax.imshow(np.array(images[i]))
            ax.set_xticks([])
            ax.set_yticks([])
            if titles and i < len(titles):
                ax.set_title(titles[i], fontsize=9)
        else:
            ax.axis("off")

    if title:
        fig.suptitle(title, fontsize=14, fontweight="bold")

    fig.tight_layout()
    plt.show()


def show_side_by_side(
    img1: Image.Image,
    img2: Image.Image,
    title1: str = "Original",
    title2: str = "Processed",
    figsize: tuple = (12, 5),
) -> None:
    """Show two images side by side."""
    fig, (ax1, ax2) = plt.subplots(1, 2, figsize=figsize)

    ax1.imshow(np.array(img1))
    ax1.set_title(title1, fontsize=12)
    ax1.set_xticks([])
    ax1.set_yticks([])

    ax2.imshow(np.array(img2), cmap="gray" if img2.mode == "L" else None)
    ax2.set_title(title2, fontsize=12)
    ax2.set_xticks([])
    ax2.set_yticks([])

    fig.tight_layout()
    plt.show()
