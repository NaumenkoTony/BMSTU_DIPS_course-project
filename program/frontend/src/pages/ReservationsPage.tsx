import { useEffect, useState } from "react";
import { getReservations, unbookHotel, type AggregatedReservationResponse } from "../api/ReservationsClient";
import { Badge, Button, Card, Container, Group, Loader, Text, Title } from "@mantine/core";
import "./ReservationsPage.css";

export default function ReservationsPage() {
  const [reservations, setReservations] = useState<AggregatedReservationResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const res = await getReservations();
        console.log(res)
        setReservations(res);
      } catch (err: any) {
        setError(err?.message ?? "Ошибка загрузки");
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, []);

  const handleCancel = async (uid: string) => {
    try {
      await unbookHotel(uid);
      setReservations((prev) => prev.filter((r) => r.reservationUid !== uid));
      const res = await getReservations();
      setReservations(res);
    } catch (err: any) {
      alert(err?.message ?? "Ошибка при отмене");
    }
  };

  const getStatusColor = (status: string) => {
    const colors: { [key: string]: string } = {
      'PAID': 'green',
      'CANCELED': 'gray',
    };
    return colors[status] || 'gray';
  };

  const formatDate = (dateString: string | number | Date) => {
    return new Date(dateString).toLocaleDateString('ru-RU', {
      day: 'numeric',
      month: 'short',
      year: 'numeric'
    });
  };

  const getReservationStatusByDate = (startDate: string, endDate: string, status: string) => {
    if (status === 'CANCELED') return 'CANCELED';

    const now = new Date();
    const start = new Date(startDate);
    const end = new Date(endDate);

    if (now > end) return 'completed';
    if (now >= start && now <= end) return 'active';
    if (start.getTime() - now.getTime() < 3 * 24 * 60 * 60 * 1000) return 'upcoming'; // ближайшие 3 дня
    return 'future';
  };

  return (
    <div className="reservations-page">
      <div className="reservations-list">
        {reservations.map((r) => {
          const dateStatus = getReservationStatusByDate(r.startDate, r.endDate, r.status);
          const cardClass = `reservation-card ${dateStatus}-card ${r.status === 'CANCELED' ? 'cancelled-card' : ''}`;
          return (
            <Card key={r.reservationUid} radius="lg" p="xl" withBorder className={cardClass}>
            <div className="card-header">
              <div className="hotel-info">
                <Text fw={600} size="xl" className="hotel-name">{r.hotel?.name ?? "Отель"}</Text>
                <Text size="sm" color="dimmed" className="hotel-address">
                  {r.hotel?.fullAddress}
                </Text>
              </div>
              <div className="hotel-rating">
                <div className="rating-badge">
                  ⭐ {r.hotel?.stars ?? "-"}
                </div>
              </div>
            </div>
            <div className="dates-section">
              <Group justify="space-between" mb="xs">
                <div className="date-item">
                  <Text size="xs" color="dimmed">Заезд</Text>
                  <Text fw={500} size="sm">{formatDate(r.startDate)}</Text>
                </div>
                <div className="date-arrow">→</div>
                <div className="date-item">
                  <Text size="xs" color="dimmed">Выезд</Text>
                  <Text fw={500} size="sm">{formatDate(r.endDate)}</Text>
                </div>
              </Group>
            </div>

            <Group justify="space-between" mt="lg">
              <div className="price-section">
                <Text size="xs" color="dimmed">Итоговая стоимость</Text>
                <Group align="center" gap="xs">
                  <Text fw={700} size="xl" className="price">
                    {r.payment?.price} ₽
                  </Text>
                  <Badge
                    color={getStatusColor(r.status)}
                    variant="light"
                    size="sm"
                    className="status-badge">
                    {r.status === 'PAID' ? 'Оплачено' : 'Возвращено'}
                  </Badge>
                </Group>
              </div>

              {r.status !== 'CANCELED' && (
                <Button
                  color="red"
                  variant="outline"
                  onClick={() => handleCancel(r.reservationUid)}
                  className="cancel-btn"
                  size="sm">
                  Отменить
                </Button>)}
            </Group>
          </Card>
        )})}
      </div>
    </div>
  );
}
