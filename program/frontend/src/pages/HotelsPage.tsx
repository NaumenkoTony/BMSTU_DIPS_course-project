import { useEffect, useMemo, useState } from "react";
import { getHotels, type HotelResponse, type HotelsPaginationResponse } from "../api/hotelsClient";
import BookHotelForm from "../components/BookHotelForm";
import "./HotelsPage.css";
import type { CreateReservationResponse } from "../api/ReservationsClient";
import { IconSearch, IconX, IconStar } from "@tabler/icons-react";

const HotelsTable = ({ hotels }: { hotels: HotelResponse[] }) => {
  const [filters, setFilters] = useState({
    country: '',
    city: '',
    address: '',
    name: '',
    minStars: '',
    maxPrice: ''
  });

  const handleFilterChange = (key: string, value: string) => {
    setFilters(prev => ({ ...prev, [key]: value }));
  };

  const clearFilters = () => {
    setFilters({
      country: '',
      city: '',
      address: '',
      name: '',
      minStars: '',
      maxPrice: ''
    });
  };

  const [sortConfig, setSortConfig] = useState<{
    key: keyof HotelResponse | null;
    direction: 'asc' | 'desc';
  }>({ key: null, direction: 'asc' });

  const sortedAndFilteredHotels = useMemo(() => {
    let filtered = hotels.filter(hotel => {
      return (
        (filters.country === '' || hotel.country.toLowerCase().includes(filters.country.toLowerCase())) &&
        (filters.city === '' || hotel.city.toLowerCase().includes(filters.city.toLowerCase())) &&
        (filters.address === '' || (hotel.address && hotel.address.toLowerCase().includes(filters.address.toLowerCase()))) &&
        (filters.name === '' || hotel.name.toLowerCase().includes(filters.name.toLowerCase())) &&
        (filters.minStars === '' || (hotel.stars && hotel.stars >= Number(filters.minStars))) &&
        (filters.maxPrice === '' || (hotel.price && hotel.price <= Number(filters.maxPrice)))
      );
    });

    if (sortConfig.key) {
      filtered.sort((a, b) => {
        let aValue = a[sortConfig.key!];
        let bValue = b[sortConfig.key!];

        if (aValue === undefined || aValue === null) aValue = '';
        if (bValue === undefined || bValue === null) bValue = '';

        if (typeof aValue === 'string' && typeof bValue === 'string') {
          return sortConfig.direction === 'asc'
            ? aValue.localeCompare(bValue)
            : bValue.localeCompare(aValue);
        } else {
          return sortConfig.direction === 'asc'
            ? (aValue as number) - (bValue as number)
            : (bValue as number) - (aValue as number);
        }
      });
    }

    return filtered;
  }, [hotels, filters, sortConfig]);

  const handleSort = (key: keyof HotelResponse) => {
    setSortConfig(prev => {
      if (prev.key === key) {
        if (prev.direction === 'asc') {
          return { key, direction: 'desc' };
        } else {
          return { key: null, direction: 'asc' };
        }
      }
      return { key, direction: 'asc' };
    });
  };

  const getSortIndicator = (key: keyof HotelResponse) => {
    if (sortConfig.key !== key) return '↕';
    return sortConfig.direction === 'asc' ? '↑' : '↓';
  };
  
  const [bookModalOpen, setBookModalOpen] = useState(false);
  const [selectedHotel, setSelectedHotel] = useState<HotelResponse | null>(null);

  const handleOpenBook = (hotel: HotelResponse) => {
    setSelectedHotel(hotel);
    setBookModalOpen(true);
  };

  const handleBooked = (info: CreateReservationResponse) => {
    console.log("Booking completed:", info);
  };

  return (
    <div className="hotels-container">
      <div className="table-header">
        <div className="header-content">
          <div className="title">Отели</div>
          <div className="filters-row">
            <div className="filter-input">
              <IconSearch size={18} className="filter-icon" />
              <input
                type="text"
                value={filters.country}
                onChange={(e) => handleFilterChange('country', e.target.value)}
                placeholder="Страна..."
                className="filter-field"
              />
            </div>

            <div className="filter-input">
              <IconSearch size={18} className="filter-icon" />
              <input
                type="text"
                value={filters.city}
                onChange={(e) => handleFilterChange('city', e.target.value)}
                placeholder="Город..."
                className="filter-field"
              />
            </div>

            <div className="filter-input">
              <IconSearch size={18} className="filter-icon" />
              <select
                value={filters.minStars}
                onChange={(e) => handleFilterChange('minStars', e.target.value)}
                className="filter-field"
                style={{ paddingLeft: '40px', appearance: 'none', backgroundImage: 'none' }}
              >
                <option value="">Звезды (не менее)</option>
                <option value="1">1</option>
                <option value="2">2</option>
                <option value="3">3</option>
                <option value="4">4</option>
                <option value="5">5</option>
              </select>
            </div>

            <div className="filter-input">
              <IconSearch size={18} className="filter-icon" />
              <input
                type="number"
                value={filters.maxPrice}
                onChange={(e) => handleFilterChange('maxPrice', e.target.value)}
                placeholder="Цена до..."
                className="filter-field"
              />
            </div>

            <div className="filter-actions">
              <button onClick={clearFilters} className="clear-filters-btn">
                <IconX size={16} />
                Очистить
              </button>
              <span className="results-badge">
                Найдено: {sortedAndFilteredHotels.length}
              </span>
            </div>
          </div>
        </div>
      </div>

      <div className="table-wrapper">
        <table className="hotels-table">
          <thead>
            <tr>
              <th onClick={() => handleSort('country')} className="sortable">
                Страна {getSortIndicator('country')}
              </th>
              <th onClick={() => handleSort('city')} className="sortable">
                Город {getSortIndicator('city')}
              </th>
              <th onClick={() => handleSort('name')} className="sortable">
                Отель {getSortIndicator('name')}
              </th>
              <th onClick={() => handleSort('stars')} className="sortable">
                Звезды {getSortIndicator('stars')}
              </th>
              <th onClick={() => handleSort('price')} className="sortable">
                Цена {getSortIndicator('price')}
              </th>
              <th>Действия</th>
            </tr>
          </thead>
          <tbody>
            {sortedAndFilteredHotels.map((hotel) => (
              <tr key={hotel.id} className="hotel-row">
                <td>
                  <div className="country-cell">
                    <span className="country-name">{hotel.country}</span>
                  </div>
                </td>
                <td>
                  <div className="city-cell">
                    {hotel.city}
                  </div>
                </td>
                <td>
                  <div className="hotel-name-cell">
                    <div className="hotel-name">{hotel.name}</div>
                    {hotel.address && <div className="hotel-address">{hotel.address}</div>}
                  </div>
                </td>
                <td>
                  <div className="stars-cell">
                    {hotel.stars ? (
                      <div className="stars-rating">
                        <IconStar size={16} className="star-icon" />
                        <span className="stars-count">{hotel.stars}</span>
                      </div>
                    ) : (
                      <span className="no-stars">—</span>
                    )}
                  </div>
                </td>
                <td>
                  <div className="price-cell">
                    {hotel.price ? (
                      <>
                        <span className="price">{hotel.price.toLocaleString('ru-RU')}</span>
                        <span className="currency">₽</span>
                      </>
                    ) : (
                      <span className="no-price">—</span>
                    )}
                  </div>
                </td>
                <td>
                  <button className="book-btn" onClick={() => handleOpenBook(hotel)}>
                    Забронировать
                  </button>
                </td>
              </tr>
            ))}
            {selectedHotel && (
              <BookHotelForm
                hotelUid={selectedHotel.hotelUid}
                hotelName={selectedHotel.name}
                opened={bookModalOpen}
                onClose={() => setBookModalOpen(false)}
                onBooked={handleBooked}
              />
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default function HotelsPage() {
  const [hotelsData, setHotelsData] = useState<HotelsPaginationResponse>({
    items: [],
    totalElements: 0,
    totalPages: 0
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  useEffect(() => {
    const fetchHotels = async () => {
      try {
        console.log("Loading hotels for page", currentPage, "with page size", pageSize);
        setLoading(true);
        const data = await getHotels(currentPage, pageSize);
        setHotelsData({
          items: data.items,
          totalElements: data.totalElements,
          totalPages: Math.ceil(data.totalElements / pageSize)
        });
      } catch (err) {
        console.error("Failed to load hotels:", err);
        setError("Не удалось загрузить отели");
      } finally {
        setLoading(false);
      }
    };

    fetchHotels();
  }, [currentPage, pageSize]);

  const handlePageChange = (newPage: number) => {
    setCurrentPage(newPage);
  };

  const handlePageSizeChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const newSize = Number(e.target.value);
    setPageSize(newSize);
    setCurrentPage(1);
  };

  if (loading) {
    return <div className="hotels-loading">Загрузка отелей...</div>;
  }

  if (error) {
    return <div className="hotels-error">{error}</div>;
  }

  const PaginationNumbers = () => {
    const totalPages = hotelsData.totalPages;
    const current = currentPage;

    let startPage = Math.max(1, current - 3);
    let endPage = Math.min(totalPages, current + 3);

    if (current <= 4) {
      endPage = Math.min(7, totalPages);
    }

    if (current >= totalPages - 3) {
      startPage = Math.max(1, totalPages - 6);
    }

    const pages = [];
    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }

    return (
      <div className="pagination-numbers">
        {startPage > 1 && (
          <>
            <button onClick={() => handlePageChange(1)} className="pagination-number">1</button>
            {startPage > 2 && <span className="pagination-ellipsis">...</span>}
          </>
        )}

        {pages.map(page => (
          <button
            key={page}
            className={`pagination-number ${currentPage === page ? "active" : ""}`}
            onClick={() => handlePageChange(page)}
          >
            {page}
          </button>
        ))}

        {endPage < totalPages && (
          <>
            {endPage < totalPages - 1 && <span className="pagination-ellipsis">...</span>}
            <button onClick={() => handlePageChange(totalPages)} className="pagination-number">
              {totalPages}
            </button>
          </>
        )}
      </div>
    );
  };

  return (
    <div className="hotels-page">
      <HotelsTable hotels={hotelsData.items} />

      <div className="pagination-controls">
        <div className="page-size-select">
          <label>
            Показывать по:
            <select value={pageSize} onChange={handlePageSizeChange}>
              <option value={10}>10</option>
              <option value={20}>20</option>
              <option value={50}>50</option>
              <option value={hotelsData.totalElements || 999999}>Все</option>
            </select>
          </label>
        </div>
        {hotelsData.totalPages > 1 && (
          <div className="pagination">
            <button
              onClick={() => handlePageChange(1)}
              disabled={currentPage === 1}
              className="pagination-btn"
            >
              «
            </button>

            <button
              onClick={() => handlePageChange(currentPage - 1)}
              disabled={currentPage === 1}
              className="pagination-btn"
            >
              ←
            </button>

            <PaginationNumbers />

            <button
              onClick={() => handlePageChange(currentPage + 1)}
              disabled={currentPage === hotelsData.totalPages}
              className="pagination-btn"
            >
              →
            </button>

            <button
              onClick={() => handlePageChange(hotelsData.totalPages)}
              disabled={currentPage === hotelsData.totalPages}
              className="pagination-btn"
            >
              »
            </button>
          </div>
        )}
      </div>
    </div>
  );
}